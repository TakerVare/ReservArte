<script setup lang="ts">
import { computed } from 'vue';
import { useI18n } from 'vue-i18n';
import { Button } from '@components/ui/button';
import { Text } from '@components/ui/text';
import type { ServiceOption } from '@features/services/api/services.api';

/**
 * Servicios que presta un empleado (4.1b; sin diseño en Figma, con el estilo de la
 * app): el catálogo activo agrupado por categoría, con una casilla por servicio.
 * La API reemplaza el conjunto entero, así que se guarda todo de una vez. El nivel
 * de destreza no se muestra (decisión de Guillermo, 2-oct): las altas nacen con 1.
 */
const props = withDefaults(defineProps<{ services: ServiceOption[]; busy?: boolean }>(), {
  busy: false,
});

const selected = defineModel<number[]>({ required: true });
const emit = defineEmits<{ save: [serviceIds: number[]] }>();

const { t } = useI18n();

const groups = computed(() => {
  const byCategory = new Map<string, ServiceOption[]>();
  for (const service of props.services) {
    const key = service.categoryName || t('employees.services.noCategory');
    byCategory.set(key, [...(byCategory.get(key) ?? []), service]);
  }
  return [...byCategory.entries()]
    .sort(([a], [b]) => a.localeCompare(b, 'es'))
    .map(([category, items]) => ({
      category,
      items: [...items].sort((a, b) => a.name.localeCompare(b.name, 'es')),
    }));
});

// Solo cuenta lo que está en el catálogo: un servicio retirado no se puede volver a asignar.
const catalogIds = computed(() => new Set(props.services.map((s) => s.id)));
const count = computed(() => selected.value.filter((id) => catalogIds.value.has(id)).length);

function toggle(id: number, checked: boolean) {
  selected.value = checked
    ? [...new Set([...selected.value, id])]
    : selected.value.filter((value) => value !== id);
}

function save() {
  emit(
    'save',
    selected.value.filter((id) => catalogIds.value.has(id))
  );
}
</script>

<template>
  <div class="flex flex-col gap-6">
    <div class="flex flex-col gap-2">
      <Text as="h2" size="h3">{{ t('employees.services.title') }}</Text>
      <Text size="notes" class="text-muted-foreground">{{ t('employees.services.hint') }}</Text>
    </div>

    <Text v-if="services.length === 0" size="paragraph" class="py-4 text-muted-foreground">
      {{ t('employees.services.empty') }}
    </Text>

    <template v-else>
      <div class="flex flex-wrap items-center justify-between gap-2">
        <Text as="p" size="notes" aria-live="polite" data-testid="services-count">
          {{ t('employees.services.selected', { count, total: services.length }) }}
        </Text>
        <div class="flex gap-2">
          <Button
            size="xs"
            variant="secondary"
            :disabled="busy"
            @click="selected = services.map((s) => s.id)"
          >
            {{ t('employees.services.all') }}
          </Button>
          <Button size="xs" variant="secondary" :disabled="busy" @click="selected = []">
            {{ t('employees.services.none') }}
          </Button>
        </div>
      </div>

      <fieldset
        v-for="group in groups"
        :key="group.category"
        class="flex flex-col border-b border-border"
      >
        <legend class="w-full bg-highlight px-3 py-2">
          <Text as="span" size="h4">{{ group.category }}</Text>
        </legend>
        <label
          v-for="service in group.items"
          :key="service.id"
          class="flex cursor-pointer items-center justify-between gap-4 border-t border-border px-3 py-3 first-of-type:border-t-0"
        >
          <span class="flex items-center gap-3">
            <input
              type="checkbox"
              class="h-5 w-5 shrink-0 accent-[hsl(var(--primary))]"
              :checked="selected.includes(service.id)"
              :disabled="busy"
              @change="toggle(service.id, ($event.target as HTMLInputElement).checked)"
            />
            <Text as="span" size="paragraph">{{ service.name }}</Text>
          </span>
          <Text as="span" size="notes" class="shrink-0 text-muted-foreground">
            {{ t('employees.services.duration', { minutes: service.durationMinutes }) }}
          </Text>
        </label>
      </fieldset>

      <Button variant="primary" size="sm" class="self-start" :disabled="busy" @click="save">
        {{ t('employees.services.save') }}
      </Button>
    </template>
  </div>
</template>
