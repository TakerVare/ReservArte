import { onScopeDispose, ref, shallowRef, watch, type Ref } from 'vue';

/** Espejo de `ApiPagination` (`meta.pagination` del envelope). */
export interface Pagination {
  page: number;
  pageSize: number;
  totalCount: number;
  totalPages: number;
}

/** Lo que devuelve un listado paginado de la API, ya desenvuelto por su feature. */
export interface PagedResult<T> {
  items: T[];
  pagination: Pagination;
}

export interface DataListQuery<F> {
  search: string;
  page: number;
  pageSize: number;
  filters: F;
}

export interface UseDataListOptions<F> {
  /** Filtros iniciales (p. ej. `{ isActive: true }`). */
  filters?: F;
  pageSize?: number;
  /** Espera tras la última tecla antes de buscar. */
  debounceMs?: number;
}

const EMPTY_PAGINATION: Pagination = { page: 1, pageSize: 0, totalCount: 0, totalPages: 0 };

/**
 * Estado de un listado paginado de la API (RA-869d7fbxn): búsqueda con espera,
 * página, filtros, carga y error. Buscar o cambiar un filtro vuelve a la
 * página 1. Si llegan dos respuestas, solo cuenta la de la última petición,
 * para que una búsqueda lenta no pise a la siguiente.
 */
export function useDataList<T, F extends object = Record<string, never>>(
  fetcher: (query: DataListQuery<F>) => Promise<PagedResult<T>>,
  options: UseDataListOptions<F> = {}
) {
  const pageSize = options.pageSize ?? 20;
  const debounceMs = options.debounceMs ?? 300;

  const items = shallowRef<T[]>([]);
  const pagination = ref<Pagination>({ ...EMPTY_PAGINATION });
  const search = ref('');
  const filters = ref({ ...(options.filters ?? {}) }) as Ref<F>;
  const page = ref(1);
  const loading = ref(false);
  const error = ref<unknown>(null);

  let lastRequest = 0;
  let debounceTimer: ReturnType<typeof setTimeout> | undefined;

  async function load() {
    const request = ++lastRequest;
    loading.value = true;
    error.value = null;
    try {
      const result = await fetcher({
        search: search.value.trim(),
        page: page.value,
        pageSize,
        filters: filters.value,
      });
      if (request !== lastRequest) return;
      items.value = result.items;
      pagination.value = result.pagination;
    } catch (err) {
      if (request !== lastRequest) return;
      error.value = err;
    } finally {
      if (request === lastRequest) loading.value = false;
    }
  }

  function goToPage(target: number) {
    const last = Math.max(pagination.value.totalPages, 1);
    const next = Math.min(Math.max(target, 1), last);
    if (next === page.value) return;
    page.value = next;
    void load();
  }

  watch(search, () => {
    clearTimeout(debounceTimer);
    debounceTimer = setTimeout(() => {
      page.value = 1;
      void load();
    }, debounceMs);
  });

  watch(
    filters,
    () => {
      page.value = 1;
      void load();
    },
    { deep: true }
  );

  onScopeDispose(() => clearTimeout(debounceTimer));

  void load();

  return {
    items,
    pagination,
    search,
    filters,
    page,
    loading,
    error,
    reload: load,
    goToPage,
  };
}
