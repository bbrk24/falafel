import { build } from 'esbuild';
import heraPlugin from '@danielx/hera/esbuild';
import * as path from 'node:path';

await build({
  entryPoints: [path.join('src', 'index.js')],
  bundle: true,
  platform: 'node',
  outdir: 'dist',
  format: 'esm',
  plugins: [heraPlugin({ inlineMap: true })],
  minify: true,
  banner: { js: '#!/usr/bin/env node' },
  sourcemap: 'external',
});
