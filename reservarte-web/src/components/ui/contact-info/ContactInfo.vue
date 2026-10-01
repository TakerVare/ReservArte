<script setup lang="ts">
import type { Component } from 'vue';
import { ContactMainTitle } from '@components/ui/contact-main-title';
import { OpeningDay } from '@components/ui/opening-day';
import { OpeningHours } from '@components/ui/opening-hours';
import { ContactData } from '@components/ui/contact-data';

export interface ScheduleBlock {
  day: string;
  hours: string[];
}

export interface ContactInfoItem {
  icon: Component;
  value: string;
  href?: string;
}

defineProps<{
  scheduleTitle: string;
  schedule: ScheduleBlock[];
  contactTitle: string;
  contactItems: ContactInfoItem[];
}>();
</script>

<template>
  <!-- Anchos de Figma («Contact-Page», 387:57554): todo el ancho en móvil y 397,
       600 y 800 px desde los cortes 576, 768 y 1200, alineado con el mapa. -->
  <div
    class="mx-auto flex w-full flex-col items-stretch py-16 min-[576px]:w-[397px] md:w-[600px] min-[1200px]:w-[800px]"
  >
    <ContactMainTitle :label="scheduleTitle" />
    <template v-for="block in schedule" :key="block.day">
      <OpeningDay :label="block.day" />
      <OpeningHours v-for="hours in block.hours" :key="hours" :hours="hours" />
    </template>

    <ContactMainTitle :label="contactTitle" class="py-12" />
    <ContactData
      v-for="item in contactItems"
      :key="item.value"
      :icon="item.icon"
      :value="item.value"
      :href="item.href"
    />
  </div>
</template>
