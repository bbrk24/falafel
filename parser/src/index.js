import { parse } from './parser.hera';
import { readFile } from 'node:fs/promises';
import JSONBig from 'true-json-bigint';

const paths = process.argv.slice(2);
const result = {};

await Promise.all(
    paths.map(async (path) => {
        const data = await readFile(path, { encoding: 'utf-8' });
        const ast = parse(data);
        result[path] = ast;
    })
);

console.log(JSONBig.stringify(result));
