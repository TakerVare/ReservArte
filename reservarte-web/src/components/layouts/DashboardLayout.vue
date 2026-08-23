<script setup lang="ts">
import Sidebar from './Sidebar.vue';
import Header from './Header.vue';
import { useUiStore } from '@stores/uiStore';

const uiStore = useUiStore();
</script>

<template>
  <div class="flex h-screen overflow-hidden">
    <div
      v-if="uiStore.sidebarOpen"
      class="fixed inset-0 z-30 bg-foreground/50 md:hidden"
      @click="uiStore.toggleSidebar()"
    />

    <Sidebar />

    <div class="flex flex-1 flex-col overflow-hidden">
      <Header />
      <main class="flex-1 overflow-y-auto">
        <router-view />
      </main>
    </div>
  </div>
</template>
