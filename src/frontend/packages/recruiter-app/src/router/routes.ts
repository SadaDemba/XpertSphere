import type { RouteRecordRaw } from 'vue-router';
import {
  organizationRoleGuard,
  adminSectionGuard,
  platformAdminGuard,
  organizationAdminGuard,
} from './guards/roleGuard';

const routes: RouteRecordRaw[] = [
  // Authentication routes (with auth layout)
  {
    path: '/auth',
    component: () => import('layouts/AuthLayout.vue'),
    children: [
      {
        path: 'login',
        component: () => import('pages/auth/LoginPage.vue'),
        meta: { requiresGuest: true },
      },
      {
        path: 'register',
        component: () => import('pages/auth/RegisterPage.vue'),
        meta: { requiresGuest: true },
      },
      {
        path: 'forgot-password',
        component: () => import('pages/auth/ForgotPasswordPage.vue'),
        meta: { requiresGuest: true },
      },
    ],
  },

  // Main application routes (with layout)
  {
    path: '/',
    component: () => import('layouts/MainLayout.vue'),
    meta: { requiresAuth: true },
    children: [
      {
        path: '',
        component: () => import('pages/DashboardPage.vue'),
        meta: { title: 'Tableau de bord' },
        beforeEnter: organizationRoleGuard,
      },
      {
        path: 'jobs',
        component: () => import('pages/jobs/JobsPage.vue'),
        meta: { title: "Offres d'emploi" },
        beforeEnter: organizationRoleGuard,
      },
      {
        path: 'jobs/:id',
        component: () => import('pages/jobs/JobOfferDetailPage.vue'),
        meta: { title: "Détail de l'offre" },
        beforeEnter: organizationRoleGuard,
      },
      {
        path: 'candidates',
        component: () => import('pages/candidates/CandidatesPage.vue'),
        meta: { title: 'Candidats' },
        beforeEnter: organizationRoleGuard,
      },
      {
        path: 'candidates/:id',
        component: () => import('pages/candidates/CandidateDetailPage.vue'),
        meta: { title: 'Détail candidat' },
        beforeEnter: organizationRoleGuard,
      },
      {
        path: 'candidates/:id/cv',
        component: () => import('pages/candidates/CvViewPage.vue'),
        meta: { title: 'CV Candidat' },
        beforeEnter: organizationRoleGuard,
      },
      {
        path: 'applications',
        component: () => import('pages/applications/ApplicationsPage.vue'),
        meta: { title: 'Candidatures' },
        beforeEnter: organizationRoleGuard,
      },
      {
        path: 'applications/:id',
        component: () => import('pages/applications/ApplicationDetailPage.vue'),
        meta: { title: 'Détail candidature' },
        beforeEnter: organizationRoleGuard,
      },
      {
        path: 'interviews',
        component: () => import('pages/InterviewsPage.vue'),
        meta: { title: 'Entretiens' },
        beforeEnter: organizationRoleGuard,
      },
      {
        path: 'reports',
        component: () => import('pages/ReportsPage.vue'),
        meta: { title: 'Rapports' },
        beforeEnter: organizationRoleGuard,
      },
      {
        path: 'profile',
        component: () => import('pages/ProfilePage.vue'),
        meta: { title: 'Profil' },
      },
      {
        path: 'preferences',
        component: () => import('pages/IndexPage.vue'),
        meta: { title: 'Préférences' },
      },
      {
        path: 'help',
        component: () => import('pages/HelpPage.vue'),
        meta: { title: 'Aide' },
      },
      {
        path: 'admin/users',
        component: () => import('pages/admin/UsersPage.vue'),
        meta: { title: 'Gestion des utilisateurs', requiresAdmin: true },
        beforeEnter: adminSectionGuard,
      },
      {
        path: 'admin/organizations',
        component: () => import('pages/admin/OrganizationsPage.vue'),
        meta: { title: 'Gestion des organisations', requiresAdmin: true },
        beforeEnter: platformAdminGuard, // Only platform admins can manage organizations
      },
      {
        path: 'admin/roles',
        component: () => import('pages/admin/RolesPage.vue'),
        meta: { title: 'Gestion des rôles', requiresAdmin: true },
        beforeEnter: adminSectionGuard,
      },
      {
        path: 'admin/organization-settings',
        component: () => import('pages/admin/OrganizationSettingsPage.vue'),
        meta: { title: "Paramètres de l'organisation", requiresAdmin: true },
        // Reserved to Organization.Admin only, matching exactly the backend policy
        // (RequireOrganizationAdminRole) guarding GET/PUT /api/organizations/me/currency - not
        // adminSectionGuard (which also lets PlatformAdmin/PlatformSuperAdmin through, and would
        // then hit a 403 from the API). See configurable-salary-currency.md, décision 5.
        beforeEnter: organizationAdminGuard,
      },
      {
        path: 'unauthorized',
        component: () => import('pages/UnauthorizedPage.vue'),
        meta: { title: 'Accès non autorisé' },
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
