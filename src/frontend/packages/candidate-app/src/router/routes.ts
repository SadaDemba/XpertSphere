import type { RouteRecordRaw } from 'vue-router';

const routes: RouteRecordRaw[] = [
  // Main app routes (with layout)
  {
    path: '/',
    component: () => import('layouts/MainLayout.vue'),
    children: [
      {
        path: '',
        component: () => import('pages/JobListingsPage.vue'),
        name: 'JobListings',
      },
      {
        path: 'login',
        component: () => import('pages/LoginPage.vue'),
        name: 'Login',
      },
      {
        path: 'register',
        component: () => import('pages/RegisterPage.vue'),
        name: 'Register',
      },
      {
        path: 'jobs/:id',
        component: () => import('pages/JobDetailsPage.vue'),
        name: 'JobDetails',
      },
      {
        path: 'applications',
        component: () => import('pages/MyApplicationsPage.vue'),
        name: 'MyApplications',
        meta: { requiresAuth: true },
      },
      {
        path: 'applications/:id',
        component: () => import('pages/ApplicationDetailsPage.vue'),
        name: 'ApplicationDetails',
        meta: { requiresAuth: true },
      },
    ],
  },

  // Always leave this as last one,
  // but you can also remove it
  {
    path: '/:catchAll(.*)*',
    component: () => import('pages/ErrorNotFound.vue'),
  },
];

export default routes;
