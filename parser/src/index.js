import { parse } from './parser.hera';
import { readFile } from 'node:fs/promises';
import JSONBig from 'true-json-bigint';

const paths = process.argv.slice(2);
const result = {};

await Promise.all(
  paths.map(async (path) => {
    const data = await readFile(path, { encoding: 'utf-8' });
    const ast = parse(data, { filename: path });

    const lineCounts = [];
    for (const match of data.matchAll(/\n/gu)) {
      lineCounts.push(match.index);
    }

    result[path] = { ast, lineCounts };
  })
);

console.log(JSONBig.stringify(result));
