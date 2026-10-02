<script setup lang="ts">
import { computed, watch } from 'vue';
import { useField, useForm } from 'vee-validate';
import { toTypedSchema } from '@vee-validate/zod';
import { useI18n } from 'vue-i18n';
import { Button } from '@components/ui/button';
import { Input } from '@components/ui/input';
import { Select } from '@components/ui/select';
import { Text } from '@components/ui/text';
import { EMPLOYEE_ROLES, type EmployeeRole } from '@features/employees/types/employee.types';
import {
  employeeSchema,
  type EmployeeFormValues,
} from '@features/employees/validation/employee.schema';

/**
 * Datos de la ficha de empleado (RA-869d7fc0h), según Figma «Detalle usuario»
 * (387:56778): «Datos de usuario» con Nombre, Apellidos, Email, Teléfono, Rol y
 * Estado, y Guardar/Cancelar. Añade la fecha de alta en el centro, que la API ya
 * guarda. En el alta no hay Estado: toda ficha nace activa. Presentacional: emite
 * `submit` con los valores validados (VeeValidate + Zod, mismas reglas que la API).
 */
const props = withDefaults(
  defineProps<{
    initial?: Partial<EmployeeFormValues>;
    /** Alta: sin Estado. */
    creating?: boolean;
    busy?: boolean;
    /** Errores de la API por campo (`details` de `GEN_VALIDATION_FAILED` o el email repetido). */
    serverErrors?: Partial<Record<keyof EmployeeFormValues, string>>;
  }>(),
  { initial: undefined, creating: false, busy: false, serverErrors: undefined }
);

const emit = defineEmits<{ submit: [values: EmployeeFormValues]; cancel: [] }>();

const { t } = useI18n();

const EMPTY: EmployeeFormValues = {
  firstName: '',
  lastName: '',
  email: '',
  phone: '',
  rol: 'Employee',
  hireDate: '',
  isActive: true,
};

const { handleSubmit, resetForm, setFieldError } = useForm({
  validationSchema: toTypedSchema(employeeSchema),
  initialValues: { ...EMPTY, ...props.initial },
});

const { value: firstName, errorMessage: firstNameError } = useField<string>('firstName');
const { value: lastName, errorMessage: lastNameError } = useField<string>('lastName');
const { value: email, errorMessage: emailError } = useField<string>('email');
const { value: phone, errorMessage: phoneError } = useField<string>('phone');
const { value: rol, errorMessage: rolError } = useField<EmployeeRole>('rol');
const { value: hireDate, errorMessage: hireDateError } = useField<string>('hireDate');
const { value: isActive } = useField<boolean>('isActive');

watch(
  () => props.initial,
  (initial) => resetForm({ values: { ...EMPTY, ...initial } })
);

watch(
  () => props.serverErrors,
  (serverErrors) => {
    for (const [field, message] of Object.entries(serverErrors ?? {})) {
      setFieldError(field as keyof EmployeeFormValues, message);
    }
  }
);

const roleOptions = computed(() =>
  EMPLOYEE_ROLES.map((value) => ({ value, label: t(`employees.roles.${value}`) }))
);
const rolModel = computed({
  get: () => rol.value,
  set: (value: string | undefined) => {
    rol.value = (value as EmployeeRole) ?? 'Employee';
  },
});
const statusOptions = computed(() => [
  { value: 'active', label: t('employees.form.active') },
  { value: 'inactive', label: t('employees.form.inactive') },
]);
const statusModel = computed({
  get: () => (isActive.value ? 'active' : 'inactive'),
  set: (value: string | undefined) => {
    isActive.value = value !== 'inactive';
  },
});

const onSubmit = handleSubmit((values) => {
  if (props.busy) return;
  emit('submit', values as EmployeeFormValues);
});

// Como en Figma: etiquetas con el estilo H4 (negrita, 18 px) sobre cada campo.
const labelClasses =
  'font-sans text-[18px] font-bold leading-[normal] tracking-[0.01em] text-foreground';
const errorClasses = 'text-destructive';
</script>

<template>
  <form class="flex w-full flex-col gap-4" novalidate @submit.prevent="onSubmit">
    <Text as="h2" size="h3" class="py-2.5">{{ t('employees.form.legend') }}</Text>

    <div class="flex flex-col gap-2">
      <label for="emp-firstname" :class="labelClasses">{{ t('employees.form.firstName') }}</label>
      <Input
        id="emp-firstname"
        v-model="firstName"
        autocomplete="given-name"
        :invalid="!!firstNameError"
        :aria-describedby="firstNameError ? 'emp-firstname-error' : undefined"
        :disabled="busy"
      />
      <Text v-if="firstNameError" id="emp-firstname-error" size="notes" :class="errorClasses">
        {{ firstNameError }}
      </Text>
    </div>

    <div class="flex flex-col gap-2">
      <label for="emp-lastname" :class="labelClasses">{{ t('employees.form.lastName') }}</label>
      <Input
        id="emp-lastname"
        v-model="lastName"
        autocomplete="family-name"
        :invalid="!!lastNameError"
        :aria-describedby="lastNameError ? 'emp-lastname-error' : undefined"
        :disabled="busy"
      />
      <Text v-if="lastNameError" id="emp-lastname-error" size="notes" :class="errorClasses">
        {{ lastNameError }}
      </Text>
    </div>

    <div class="flex flex-col gap-2">
      <label for="emp-email" :class="labelClasses">{{ t('employees.form.email') }}</label>
      <Input
        id="emp-email"
        v-model="email"
        type="email"
        autocomplete="email"
        :invalid="!!emailError"
        :aria-describedby="emailError ? 'emp-email-error' : undefined"
        :disabled="busy"
      />
      <Text v-if="emailError" id="emp-email-error" size="notes" :class="errorClasses">
        {{ emailError }}
      </Text>
    </div>

    <div class="flex flex-col gap-2">
      <label for="emp-phone" :class="labelClasses">
        {{ t('employees.form.phone') }}
        <span class="font-sans text-[14px] font-normal text-muted-foreground">{{
          t('employees.form.optional')
        }}</span>
      </label>
      <Input
        id="emp-phone"
        v-model="phone"
        type="tel"
        autocomplete="tel"
        :invalid="!!phoneError"
        :aria-describedby="phoneError ? 'emp-phone-error' : undefined"
        :disabled="busy"
      />
      <Text v-if="phoneError" id="emp-phone-error" size="notes" :class="errorClasses">
        {{ phoneError }}
      </Text>
    </div>

    <div class="flex flex-col gap-2">
      <label for="emp-hiredate" :class="labelClasses">
        {{ t('employees.form.hireDate') }}
        <span class="font-sans text-[14px] font-normal text-muted-foreground">{{
          t('employees.form.optional')
        }}</span>
      </label>
      <Input
        id="emp-hiredate"
        v-model="hireDate"
        type="date"
        :invalid="!!hireDateError"
        :aria-describedby="hireDateError ? 'emp-hiredate-error' : undefined"
        :disabled="busy"
      />
      <Text v-if="hireDateError" id="emp-hiredate-error" size="notes" :class="errorClasses">
        {{ hireDateError }}
      </Text>
    </div>

    <div class="flex flex-col gap-2">
      <label for="emp-rol" :class="labelClasses">{{ t('employees.form.rol') }}</label>
      <Select
        id="emp-rol"
        v-model="rolModel"
        :options="roleOptions"
        :invalid="!!rolError"
        :disabled="busy"
      />
      <Text v-if="rolError" size="notes" :class="errorClasses">{{ rolError }}</Text>
    </div>

    <div v-if="!creating" class="flex flex-col gap-2">
      <label for="emp-status" :class="labelClasses">{{ t('employees.form.status') }}</label>
      <Select id="emp-status" v-model="statusModel" :options="statusOptions" :disabled="busy" />
    </div>

    <Text v-if="creating" size="notes" class="text-muted-foreground">
      {{ t('employees.form.invitationHint') }}
    </Text>

    <!-- Como en Figma: Guardar a la izquierda y Cancelar a la derecha. -->
    <div class="flex items-center justify-between gap-4 pt-2">
      <Button type="submit" size="sm" variant="primary" class="min-w-[106px]" :disabled="busy">
        {{ t('employees.form.save') }}
      </Button>
      <Button
        size="sm"
        variant="secondary"
        class="min-w-[106px]"
        :disabled="busy"
        @click="emit('cancel')"
      >
        {{ t('employees.form.cancel') }}
      </Button>
    </div>
  </form>
</template>
