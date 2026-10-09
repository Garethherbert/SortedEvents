// Prepare a deployment checkout from the one maintained source tree. Never copies
// credentials, .git, private local data, node_modules or native signing material.
import {cp,mkdir,readFile,writeFile,rm} from 'node:fs/promises';import path from 'node:path';import {brands} from '../brands/config.mjs';
const [key,destination]=process.argv.slice(2);if(!brands[key]||!destination)throw Error('Usage: node scripts/export-brand.mjs celebrations|live ABSOLUTE_FOLDER');const target=path.resolve(destination);if(target===process.cwd())throw Error('Use a different deployment folder.');
await mkdir(target,{recursive:true});for(const name of ['web','worker','brands','db','drizzle','tests','package.json','package-lock.json','drizzle.config.ts','.gitignore'])await cp(name,path.join(target,name),{recursive:true});
await rm(path.join(target,'tests','native-packaging.test.mjs'),{force:true});
await mkdir(path.join(target,'scripts'),{recursive:true});for(const name of ['build.mjs','dev-server.mjs','local-db.mjs','export-brand.mjs'])await cp('scripts/'+name,path.join(target,'scripts',name));
await mkdir(path.join(target,'.openai'),{recursive:true});await writeFile(path.join(target,'.openai','brand.json'),JSON.stringify({brand:key})+'\n');
await mkdir(path.join(target,'docs'),{recursive:true});await cp('docs/BRANDS.md',path.join(target,'docs','BRANDS.md'));
await writeFile(path.join(target,'README.md'),`# ${brands[key].name}\n\n${brands[key].tagline}\n\nWebsite and browser organiser built from the canonical BigDaySorted shared source using the ${key} brand configuration.\n\nInstall Node 24 or newer. Run npm ci --ignore-scripts, npm run build, then npm start. Open http://localhost:3000.\n\nSee docs/BRANDS.md for shared-source maintenance and release boundaries. Hosting manifest belongs to this existing Site; do not replace it with another brand's manifest.\n`);console.log('Prepared '+brands[key].name+' deployment checkout.');
