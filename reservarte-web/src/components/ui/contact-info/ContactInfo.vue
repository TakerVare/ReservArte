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
  <div
    class="mx-auto flex w-full max-w-[375px] flex-col items-stretch py-16 md:max-w-[600px] xl:max-w-[800px]"
  >
    <ContactMainTitle :label="scheduleTitle" />
    <template v-for="block in schedule" :key="block.day">
      <OpeningDay :label="block.day" />
      <OpeningHours v-for="hours in block.hours" :key="hours" :hours="hours" />
    </template>

    <ContactMainTitle :label="contactTitle" />
    <ContactData
      v-for="item in contactItems"
      :key="item.value"
      :icon="item.icon"
      :value="item.value"
      :href="item.href"
    />
  </div>
</template>
