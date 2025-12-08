import { fileURLToPath } from "node:url";
import { dirname } from "node:path";
import { StorybookConfig } from '@analogjs/storybook-angular';

const config: StorybookConfig = {
  "stories": [
    "../src/**/*.mdx",
    "../src/**/*.stories.@(js|jsx|mjs|ts|tsx)"
  ],
  "addons": [
    getAbsolutePath("@storybook/addon-docs")
  ],
  "framework": {
    "name": "@analogjs/storybook-angular",
    "options": {}
  },
  "docs": {}
};
export default config;

function getAbsolutePath(value: string): any {
  return dirname(fileURLToPath(import.meta.resolve(`${value}/package.json`)));
}
