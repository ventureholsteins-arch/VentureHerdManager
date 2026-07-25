import {
  createRouter,
  createWebHistory
} from 'vue-router'

import DashboardView from '../views/DashboardView.vue'
import AnimalView from '../views/AnimalView.vue'
import CalendarView from '../views/CalendarView.vue'
import DemoView from '../views/DemoView.vue'

import SettingsView from '../views/SettingsView.vue'

const isDemoOnly = import.meta.env.VITE_DEMO_ONLY === 'true'

const router = createRouter({
  history: createWebHistory(import.meta.env.BASE_URL),

  routes: [
    {
      path: '/',
      name: 'dashboard',
      component: DashboardView
    },
    {
      path: '/demo',
      name: 'demo',
      component: DemoView
    },
    {
      path: '/calendar',
      name: 'calendar',
      component: CalendarView
    },
    {
      path: '/settings',
      name: 'settings',
      component: SettingsView
    },
    {
      path: '/animals/:animalId',
      name: 'animal',
      component: AnimalView,
      props: true
    }
  ],

  scrollBehavior() {
    return {
      top: 0
    }
  }
})

router.beforeEach((to) => {
  if (isDemoOnly && to.path !== '/demo' && !sessionStorage.getItem('demo-launched')) {
    return '/demo'
  }

  return true
})

export default router