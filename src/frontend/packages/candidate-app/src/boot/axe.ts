import { boot } from 'quasar/wrappers';

export default boot(({ app }) => {
  if (process.env.DEV) {
    import('vue-axe')
      .then((VueAxeModule) => {
        const VueAxe = VueAxeModule.default;

        app.use(VueAxe, {
          config: {
            tags: ['wcag2a', 'wcag2aa', 'wcag21aa'],
          },
          clearConsoleOnUpdate: false,
          auto: true,
        });

        console.log('Vue-axe accessibility testing enabled');
      })
      .catch((error) => {
        console.warn('Failed to load vue-axe:', error);
      });
  }
});
