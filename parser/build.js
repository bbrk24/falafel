import esbuild from 'esbuild';
import hera from '@danielx/hera/esbuild-plugin';
import * as path from 'node:path';

await esbuild.build({
  entryPoints: [path.join('src', 'index.js')],
  bundle: true,
  platform: 'node',
  outdir: 'dist',
  format: 'esm',
  plugins: [hera()],
  minify: true,
  banner: { js: '#!/usr/bin/env node' },
  sourcemap: 'external',
});
