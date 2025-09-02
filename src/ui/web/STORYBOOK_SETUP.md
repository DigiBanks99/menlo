# Storybook Setup with Angular and Vite

This document describes the Storybook setup for both the `menlo-app` and `menlo-lib` projects using Angular and Vite.

## What Was Installed

1. **Storybook Core**: The latest Storybook with Angular support
2. **AnalogJS Integration**: `@analogjs/storybook-angular` for Vite integration
3. **Example Stories**: Default story files for demonstration

## Projects Configured

### 1. menlo-app (Application)
- **Storybook URL**: http://localhost:6006
- **Configuration**: `projects/menlo-app/.storybook/`
- **Stories Location**: `projects/menlo-app/src/stories/`
- **Build Output**: `dist/storybook/menlo-app/`

### 2. menlo-lib (Library)
- **Storybook URL**: http://localhost:6007
- **Configuration**: `projects/menlo-lib/.storybook/`
- **Stories Location**: `projects/menlo-lib/src/lib/` (alongside components)
- **Build Output**: `dist/storybook/menlo-lib/`

## Available Scripts

Add these scripts to your workflow:

```json
{
  "scripts": {
    "storybook": "ng run menlo-app:storybook",
    "build-storybook": "ng run menlo-app:build-storybook",
    "storybook:lib": "ng run menlo-lib:storybook",
    "build-storybook:lib": "ng run menlo-lib:build-storybook"
  }
}
```

## Usage

### Running Storybook for Development

**For menlo-app:**
```bash
npm run storybook
```

**For menlo-lib:**
```bash
npm run storybook:lib
```

### Building Storybook for Production

**For menlo-app:**
```bash
npm run build-storybook
```

**For menlo-lib:**
```bash
npm run build-storybook:lib
```

## Key Features

✅ **Vite Integration**: Uses Vite for faster development and builds
✅ **Angular Compatibility**: Full Angular support with latest features
✅ **Standalone Components**: Works with Angular standalone components
✅ **Hot Module Replacement**: Fast refresh during development
✅ **TypeScript Support**: Full TypeScript integration
✅ **Multi-project Support**: Separate Storybook instances for app and library

## Configuration Files

### Main Configuration (`main.ts`)
Located in each project's `.storybook/main.ts`:

```typescript
import { StorybookConfig } from '@analogjs/storybook-angular';

const config: StorybookConfig = {
  "stories": [
    "../src/**/*.mdx",
    "../src/**/*.stories.@(js|jsx|mjs|ts|tsx)"
  ],
  "addons": [
    "@storybook/addon-docs"
  ],
  "framework": {
    "name": "@analogjs/storybook-angular",
    "options": {}
  },
  "docs": {}
};
export default config;
```

### Preview Configuration (`preview.ts`)
Located in each project's `.storybook/preview.ts`:

```typescript
import type { Preview } from '@storybook/angular';

const preview: Preview = {
  parameters: {
    controls: {
      matchers: {
       color: /(background|color)$/i,
       date: /Date$/i,
      },
    },
  },
};

export default preview;
```

## Creating Stories

### For menlo-app Components
Create stories in `projects/menlo-app/src/stories/` or alongside your components:

```typescript
import type { Meta, StoryObj } from '@storybook/angular';
import { MyComponent } from './my-component';

const meta: Meta<MyComponent> = {
  title: 'App/MyComponent',
  component: MyComponent,
  parameters: {
    layout: 'centered',
  },
  tags: ['autodocs'],
};

export default meta;
type Story = StoryObj<MyComponent>;

export const Default: Story = {
  args: {},
};
```

### For menlo-lib Components
Create stories alongside your library components in `projects/menlo-lib/src/lib/`:

```typescript
import type { Meta, StoryObj } from '@storybook/angular';
import { MenloLib } from './menlo-lib';

const meta: Meta<MenloLib> = {
  title: 'Menlo Lib/MenloLib',
  component: MenloLib,
  parameters: {
    layout: 'centered',
  },
  tags: ['autodocs'],
};

export default meta;
type Story = StoryObj<MenloLib>;

export const Default: Story = {
  args: {},
};
```

## Benefits of This Setup

1. **Performance**: Vite provides faster builds and hot reloading
2. **Modern Tooling**: Latest Angular and Storybook features
3. **Separation of Concerns**: Separate Storybook instances for app and library
4. **Documentation**: Automatic documentation generation with Compodoc
5. **TypeScript**: Full type safety and IntelliSense support

## Troubleshooting

### Common Issues

1. **Port Conflicts**: If ports 6006 or 6007 are in use, update the port in `angular.json`
2. **Import Errors**: Make sure all dependencies are properly imported in stories
3. **Path Issues**: Use relative paths for imports within stories

### Dependencies

The following packages were automatically installed:
- `@analogjs/storybook-angular`
- `@storybook/angular`
- `@storybook/addon-docs`
- `@compodoc/compodoc`
- `storybook`

## Next Steps

1. **Add More Stories**: Create stories for your existing components
2. **Configure Addons**: Add more Storybook addons as needed
3. **CI/CD Integration**: Set up automated Storybook builds
4. **Documentation**: Enhance component documentation with JSDoc comments
5. **Testing**: Add visual regression testing with Chromatic or similar tools

## Resources

- [Storybook for Angular](https://storybook.js.org/docs/get-started/frameworks/angular)
- [AnalogJS Storybook Integration](https://analogjs.org/docs/packages/storybook-angular)
- [Angular Best Practices](https://angular.dev/style-guide)
- [Original Guide](https://dev.to/brandontroberts/using-storybook-with-angular-and-vite-48ga)
