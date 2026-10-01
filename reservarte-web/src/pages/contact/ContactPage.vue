<script setup lang="ts">
import { useI18n } from 'vue-i18n';
import { Banner } from '@components/ui/banner';
import { MapContact } from '@components/ui/map-contact';
import { ContactInfo, type ContactInfoItem } from '@components/ui/contact-info';
import { centerContact } from '@/config/center';
import PhoneIcon from '@assets/icons/contact-phone.svg';
import InstagramIcon from '@assets/icons/oauth-instagram.svg';
import logo from '@assets/images/Logo_Recto_More_Than_Brows_SIN_fondo.png';

/**
 * Contacto (destino «Contacto» del BottomNav, Figma `387:56672`; RA-869faaunu).
 * Pública. Los datos del centro salen de `config/center.ts` hasta que la API
 * los exponga.
 */

const { t } = useI18n();

const contactItems: ContactInfoItem[] = [
  { icon: PhoneIcon, value: centerContact.phone.display, href: centerContact.phone.href },
  {
    icon: InstagramIcon,
    value: centerContact.instagram.display,
    href: centerContact.instagram.href,
  },
];
</script>

<template>
  <div class="flex w-full flex-col items-center">
    <Banner :logo-src="logo" logo-alt="More Than Brows" />
    <h1 class="sr-only">{{ t('contact.title') }}</h1>
    <main class="w-full">
      <MapContact
        v-if="centerContact.address"
        :address="centerContact.address"
        :title="t('contact.mapTitle')"
      />
      <ContactInfo
        :schedule-title="t('contact.scheduleTitle')"
        :schedule="centerContact.schedule"
        :contact-title="t('contact.contactTitle')"
        :contact-items="contactItems"
      />
    </main>
  </div>
</template>
