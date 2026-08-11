import {
  createRouter,
  createWebHistory
} from 'vue-router'

import DashboardView from '../views/DashboardView.vue'
import AnimalView from '../views/AnimalView.vue'
import CalendarView from '../views/CalendarView.vue'
import AnimalCreateView from '../views/AnimalCreateView.vue'
import ReportsView from '../views/ReportsView.vue'
import PrintReportsView from '../views/PrintReportsView.vue'
import SireCatalogView from '../views/SireCatalogView.vue'
import HerdDataView from '../views/HerdDataView.vue'
import AuditView from '../views/AuditView.vue'
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
      path: '/animals/new',
      name: 'animal-create',
      component: AnimalCreateView,
    },
    {
      path: '/calendar',
      name: 'calendar',
      component: CalendarView
    },
    {
      path: '/reports',
      name: 'reports',
      component: ReportsView
    },
    {
      path: '/embryos',
      name: 'embryos',
      component: ReportsView
    },
    {
      path: '/shows',
      name: 'shows',
      component: ReportsView
    },
    {
      path: '/reports/print',
      name: 'print-reports',
      component: PrintReportsView
    },
    {
      path: '/reports/sires',
      name: 'sire-catalog',
      component: SireCatalogView
    },
    {
      path: '/reports/herd-data',
      name: 'herd-data',
      component: HerdDataView
    },
    {
      path: '/reports/audit',
      name: 'audit',
      component: AuditView
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

  scrollBehavior(_to, _from, savedPosition) {
    if (savedPosition) {
      return savedPosition
    }

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
