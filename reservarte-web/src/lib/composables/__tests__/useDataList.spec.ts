import { afterEach, beforeEach, describe, expect, it, vi } from 'vitest';
import { effectScope, nextTick } from 'vue';
import { useDataList, type DataListQuery, type PagedResult } from '../useDataList';

interface Row {
  id: number;
}

function page(items: number[], current = 1, totalPages = 1): PagedResult<Row> {
  return {
    items: items.map((id) => ({ id })),
    pagination: { page: current, pageSize: 20, totalCount: items.length, totalPages },
  };
}

function deferred<T>() {
  let resolve!: (value: T) => void;
  let reject!: (reason: unknown) => void;
  const promise = new Promise<T>((res, rej) => {
    resolve = res;
    reject = rej;
  });
  return { promise, resolve, reject };
}

const flush = async () => {
  await nextTick();
  await Promise.resolve();
  await nextTick();
};

let scope: ReturnType<typeof effectScope>;

beforeEach(() => {
  vi.useFakeTimers();
  scope = effectScope();
});

afterEach(() => {
  scope.stop();
  vi.useRealTimers();
});

describe('useDataList', () => {
  it('carga la primera página al empezar, con los filtros iniciales', async () => {
    const fetcher = vi.fn<(q: DataListQuery<{ isActive: boolean }>) => Promise<PagedResult<Row>>>(
      async () => page([1, 2])
    );
    const list = scope.run(() =>
      useDataList(fetcher, { filters: { isActive: true }, pageSize: 10 })
    )!;
    expect(list.loading.value).toBe(true);
    await flush();

    expect(fetcher).toHaveBeenCalledWith({
      search: '',
      page: 1,
      pageSize: 10,
      filters: { isActive: true },
    });
    expect(list.items.value.map((r) => r.id)).toEqual([1, 2]);
    expect(list.loading.value).toBe(false);
  });

  it('busca tras la espera, con el texto recortado y desde la página 1', async () => {
    const fetcher = vi.fn(async () => page([1], 1, 3));
    const list = scope.run(() => useDataList<Row>(fetcher, { debounceMs: 300 }))!;
    await flush();
    list.goToPage(2);
    await flush();

    list.search.value = 'la';
    list.search.value = ' laura ';
    await nextTick(); // el watch de Vue programa la espera en el tick siguiente
    vi.advanceTimersByTime(299);
    expect(fetcher).toHaveBeenCalledTimes(2);

    vi.advanceTimersByTime(1);
    await flush();
    expect(fetcher).toHaveBeenCalledTimes(3);
    expect(fetcher).toHaveBeenLastCalledWith(expect.objectContaining({ search: 'laura', page: 1 }));
  });

  it('cambiar un filtro vuelve a la página 1 y recarga sin esperar', async () => {
    const fetcher = vi.fn(async () => page([1], 1, 3));
    const list = scope.run(() => useDataList<Row, { isActive?: boolean }>(fetcher))!;
    await flush();
    list.goToPage(3);
    await flush();

    list.filters.value.isActive = false;
    await flush();

    expect(fetcher).toHaveBeenLastCalledWith(
      expect.objectContaining({ page: 1, filters: { isActive: false } })
    );
  });

  it('goToPage se queda dentro de las páginas que hay', async () => {
    const fetcher = vi.fn(async (q: DataListQuery<object>) => page([1], q.page, 2));
    const list = scope.run(() => useDataList<Row>(fetcher))!;
    await flush();

    list.goToPage(5);
    await flush();
    expect(list.page.value).toBe(2);

    list.goToPage(0);
    await flush();
    expect(list.page.value).toBe(1);
    expect(fetcher).toHaveBeenCalledTimes(3);
  });

  it('una respuesta lenta no pisa a la de la petición siguiente', async () => {
    const slow = deferred<PagedResult<Row>>();
    const fast = deferred<PagedResult<Row>>();
    const fetcher = vi.fn().mockReturnValueOnce(slow.promise).mockReturnValueOnce(fast.promise);
    const list = scope.run(() => useDataList<Row>(fetcher))!;

    void list.reload();
    fast.resolve(page([2]));
    await flush();
    slow.resolve(page([1]));
    await flush();

    expect(list.items.value.map((r) => r.id)).toEqual([2]);
    expect(list.loading.value).toBe(false);
  });

  it('guarda el error y lo limpia al recargar con éxito', async () => {
    const fetcher = vi
      .fn()
      .mockRejectedValueOnce(new Error('caída'))
      .mockResolvedValueOnce(page([1]));
    const list = scope.run(() => useDataList<Row>(fetcher))!;
    await flush();
    expect(list.error.value).toBeInstanceOf(Error);
    expect(list.loading.value).toBe(false);

    await list.reload();
    expect(list.error.value).toBeNull();
    expect(list.items.value).toHaveLength(1);
  });
});
