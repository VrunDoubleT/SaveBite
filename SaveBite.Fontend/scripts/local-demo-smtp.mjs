// Local-only SMTP inbox for screenshot OTP flows. No email is delivered externally.
import net from 'node:net';
import { mkdir, writeFile } from 'node:fs/promises';
const inbox = '/tmp/savebite-demo-mail';
await mkdir(inbox, { recursive: true });
net.createServer(socket => {
  socket.setEncoding('utf8');
  socket.write('220 localhost Demo SMTP\r\n');
  let buffer = '', data = false, lines = [], auth = 0;
  socket.on('data', async chunk => {
    buffer += chunk;
    while (buffer.includes('\r\n')) {
      const index = buffer.indexOf('\r\n');
      const line = buffer.slice(0, index); buffer = buffer.slice(index + 2);
      if (data) {
        if (line === '.') {
          await writeFile(`${inbox}/${Date.now()}.eml`, lines.join('\r\n'));
          lines = []; data = false; socket.write('250 Message accepted\r\n');
        } else lines.push(line);
      } else if (auth) {
        if (--auth) socket.write('334 UGFzc3dvcmQ6\r\n'); else socket.write('235 Authentication successful\r\n');
      } else if (/^EHLO|^HELO/i.test(line)) socket.write('250-localhost\r\n250-AUTH PLAIN LOGIN\r\n250 SIZE 10485760\r\n');
      else if (/^AUTH PLAIN /i.test(line)) socket.write('235 Authentication successful\r\n');
      else if (/^AUTH /i.test(line)) { auth = /^AUTH LOGIN/i.test(line) ? 2 : 1; socket.write('334 VXNlcm5hbWU6\r\n'); }
      else if (/^DATA/i.test(line)) { data = true; socket.write('354 End with a dot\r\n'); }
      else if (/^QUIT/i.test(line)) socket.end('221 Bye\r\n');
      else socket.write('250 OK\r\n');
    }
  });
}).listen(1025, '127.0.0.1', () => console.log('Local demo inbox listening on 127.0.0.1:1025'));
