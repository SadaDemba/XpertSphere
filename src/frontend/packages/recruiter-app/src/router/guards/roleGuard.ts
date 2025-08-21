import type { NavigationGuardNext, RouteLocationNormalized } from 'vue-router';
import { useAuthStore } from '../../stores/authStore';
import { UserRole, PlatformRoles, OrganizationRoles } from '../../models/auth';

export interface RouteRoleGuard {
  roles?: string[];
  requirePlatformRole?: boolean;
  requireOrganizationRole?: boolean;
  requireOrganizationAdmin?: boolean;
}

export function createRoleGuard(options: RouteRoleGuard) {
  return (
    to: RouteLocationNormalized,
    from: RouteLocationNormalized,
    next: NavigationGuardNext,
  ) => {
    const authStore = useAuthStore();

    // Check if user is authenticated
    if (!authStore.isAuthenticated) {
      return next('/auth/login');
    }

    // Check specific roles if provided
    if (options.roles && options.roles.length > 0) {
      if (!authStore.hasAnyRole(options.roles)) {
        return next('/unauthorized');
      }
    }

    // Check for platform role requirement
    if (options.requirePlatformRole) {
      if (!authStore.hasAnyRole(PlatformRoles)) {
        return next('/unauthorized');
      }
    }

    // Check for organization role requirement
    if (options.requireOrganizationRole) {
      if (!authStore.hasAnyRole(OrganizationRoles)) {
        return next('/unauthorized');
      }
    }

    // Check for organization admin requirement
    if (options.requireOrganizationAdmin) {
      if (!authStore.hasRole(UserRole.OrganizationAdmin)) {
        return next('/unauthorized');
      }
    }

    // All checks passed
    next();
  };
}

// Pre-defined guards for common scenarios
export const platformAdminGuard = createRoleGuard({
  requirePlatformRole: true,
});

export const organizationRoleGuard = createRoleGuard({
  requireOrganizationRole: true,
});

export const organizationAdminGuard = createRoleGuard({
  requireOrganizationAdmin: true,
});

export const adminSectionGuard = createRoleGuard({
  roles: [...PlatformRoles, UserRole.OrganizationAdmin],
});
