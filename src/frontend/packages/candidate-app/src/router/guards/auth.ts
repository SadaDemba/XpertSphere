import type { NavigationGuard } from 'vue-router';
import { useAuthStore } from '../../stores/authStore';

export const authGuard: NavigationGuard = async (to, from, next) => {
  const authStore = useAuthStore();

  // Initialize auth store if not already done
  if (!authStore.isInitialized) {
    await authStore.initialize();
  }

  // Check if route requires authentication
  if (to.meta.requiresAuth && !authStore.isAuthenticated) {
    // Redirect to login with return URL
    next({
      path: '/login',
      query: { redirect: to.fullPath },
    });
    return;
  }

  next();
};
