import type { RouteLocationNormalized, NavigationGuardNext } from 'vue-router';
import { useAuthStore } from '../../stores/authStore';
import { PlatformRoles } from '../../models/auth';

/**
 * Authentication guard to protect routes
 */
export async function authGuard(
  to: RouteLocationNormalized,
  from: RouteLocationNormalized,
  next: NavigationGuardNext,
) {
  const authStore = useAuthStore();
  // Allow access to login and other auth pages
  const publicPages = ['/auth/login', '/auth/register', '/auth/forgot-password'];
  const isPublicPage = publicPages.includes(to.path);

  if (isPublicPage) {
    // If already authenticated and trying to access login page, redirect based on role
    if (authStore.isAuthenticated && to.path === '/auth/login') {
      // Redirect PlatformRoles to admin/users, others to jobs
      const redirectPath = authStore.hasAnyRole(PlatformRoles) ? '/admin/users' : '/jobs';
      next(redirectPath);
      return;
    }
    next();
    return;
  }

  // Wait for initialization to complete
  await authStore.initialize();

  // Check if user is authenticated
  if (!authStore.isAuthenticated) {
    // Redirect to login with return URL
    next({
      path: '/auth/login',
      query: { redirect: to.fullPath },
    });
    return;
  }

  // Check if user has permission for recruiter app (exclude candidates)
  if (authStore.isCandidate || !authStore.isInternalUser) {
    // User doesn't have permission, logout and redirect
    await authStore.logout();
    next({
      path: '/auth/login',
      query: {
        redirect: to.fullPath,
        error: 'unauthorized',
      },
    });
    return;
  }

  // Check admin-only routes (management roles required)
  if (to.meta.requiresAdmin && !authStore.canManageUsers) {
    // Redirect to appropriate dashboard based on role
    const redirectPath = authStore.hasAnyRole(PlatformRoles) ? '/admin/users' : '/jobs';
    next(redirectPath);
    return;
  }

  // User is authenticated and authorized
  next();
}

/**
 * Role-based guard for admin-only routes
 */
export async function adminGuard(
  to: RouteLocationNormalized,
  from: RouteLocationNormalized,
  next: NavigationGuardNext,
) {
  const authStore = useAuthStore();

  if (!authStore.isAuthenticated) {
    next({
      path: '/auth/login',
      query: { redirect: to.fullPath },
    });
    return;
  }

  if (!authStore.canManageUsers) {
    // Redirect to dashboard if not authorized for admin functions
    next('/jobs');
    return;
  }

  next();
}
