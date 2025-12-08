import { defineConfig } from './src/ui/web/node_modules/vitest/config';

export default defineConfig({
    test: {
        globals: true,
        environment: 'jsdom',
        projects: [
            "src/ui/web/*"
        ]
    }
});
