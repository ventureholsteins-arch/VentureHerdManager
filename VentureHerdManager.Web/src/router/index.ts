import {
  createRouter,
  createWebHistory
} from 'vue-router'

import DashboardView from '../views/DashboardView.vue'
import AnimalView from '../views/AnimalView.vue'
import CalendarView from '../views/CalendarView.vue'

const router = createRouter({
  history: createWebHistory(import.meta.env.BASE_URL),

  routes: [
    {
      path: '/',
      name: 'dashboard',
      component: DashboardView
    },
    {
      path: '/calendar',
      name: 'calendar',
      component: CalendarView
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

export default router