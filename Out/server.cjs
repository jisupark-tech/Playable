// server.cjs
const express = require('express');
const path = require('path');

const app = express();
const root = __dirname;            // index.html이 있는 폴더
const port = process.env.PORT || 8080;

// 공통 헤더(원하면 캐시시간 수정)
app.use((req, res, next) => {
  res.setHeader('Access-Control-Allow-Origin', '*');
  res.setHeader('Cache-Control', 'max-age=3600');
  next();
});

// ==== Brotli/Gzip 인코딩 헤더 강제 부여 (URL 확장자 기준) ====
app.use((req, res, next) => {
  const p = req.path;

  if (p.endsWith('.wasm.br')) {
    res.setHeader('Content-Type', 'application/wasm');
    res.setHeader('Content-Encoding', 'br');
  } else if (p.endsWith('.js.br')) {
    res.setHeader('Content-Type', 'application/javascript; charset=utf-8');
    res.setHeader('Content-Encoding', 'br');
  } else if (p.endsWith('.data.br')) {
    res.setHeader('Content-Type', 'application/octet-stream');
    res.setHeader('Content-Encoding', 'br');
  } else if (p.endsWith('.symbols.json.br')) {
    res.setHeader('Content-Type', 'application/json; charset=utf-8');
    res.setHeader('Content-Encoding', 'br');
  } else if (p.endsWith('.wasm.gz')) {
    res.setHeader('Content-Type', 'application/wasm');
    res.setHeader('Content-Encoding', 'gzip');
  } else if (p.endsWith('.js.gz')) {
    res.setHeader('Content-Type', 'application/javascript; charset=utf-8');
    res.setHeader('Content-Encoding', 'gzip');
  } else if (p.endsWith('.data.gz')) {
    res.setHeader('Content-Type', 'application/octet-stream');
    res.setHeader('Content-Encoding', 'gzip');
  }
  next();
});

// 정적 파일 서빙 (index.html, Build/, TemplateData/)
app.use(express.static(root, {
  setHeaders: (res, filePath) => {
    // 비압축 .wasm MIME 보정
    if (filePath.endsWith('.wasm')) {
      res.setHeader('Content-Type', 'application/wasm');
    }
  }
}));

app.listen(port, '0.0.0.0', () => {
  console.log(`WebGL server running at:
  - http://127.0.0.1:${port}
  - http://0.0.0.0:${port} (LAN)`);
});
// app.listen(port, () => {
//   console.log(`WebGL server running at:
//   - http://127.0.0.1:${port}
//   - http://0.0.0.0:${port} (LAN)`);
// });
