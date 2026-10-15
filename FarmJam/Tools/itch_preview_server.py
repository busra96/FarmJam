"""Local-only WebGL preview with correct MIME/gzip headers; never uploads files."""
import argparse
import functools
import http.server
import mimetypes


class Handler(http.server.SimpleHTTPRequestHandler):
    def guess_type(self, path):
        if path.endswith('.gz'):
            path = path[:-3]
        return mimetypes.guess_type(path)[0] or 'application/octet-stream'

    def end_headers(self):
        if self.path.split('?', 1)[0].endswith('.gz'):
            self.send_header('Content-Encoding', 'gzip')
        self.send_header('Cache-Control', 'no-store')
        super().end_headers()


if __name__ == '__main__':
    parser = argparse.ArgumentParser()
    parser.add_argument('directory')
    parser.add_argument('--port', type=int, default=8794)
    args = parser.parse_args()
    mimetypes.add_type('application/wasm', '.wasm')
    server = http.server.ThreadingHTTPServer(('127.0.0.1', args.port), functools.partial(Handler, directory=args.directory))
    print(f'Farm Jam preview: http://127.0.0.1:{args.port}', flush=True)
    server.serve_forever()
