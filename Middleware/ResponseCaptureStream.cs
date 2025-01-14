using System.Text;

namespace KothBackend.Middleware
{
    public class ResponseCaptureStream : Stream
    {
        private readonly Stream _originalBody;
        private readonly MemoryStream _captureStream;

        public ResponseCaptureStream(Stream originalBody)
        {
            _originalBody = originalBody;
            _captureStream = new MemoryStream();
        }

        public string GetCapturedContent()
        {
            _captureStream.Position = 0;
            using var reader = new StreamReader(_captureStream, Encoding.UTF8);
            return reader.ReadToEnd();
        }

        public override bool CanRead => true;
        public override bool CanSeek => true;
        public override bool CanWrite => true;
        public override long Length => _captureStream.Length;
        public override long Position
        {
            get => _captureStream.Position;
            set => _captureStream.Position = value;
        }

        public override void Flush()
        {
            _originalBody.Flush();
            _captureStream.Flush();
        }

        public override int Read(byte[] buffer, int offset, int count)
        {
            return _originalBody.Read(buffer, offset, count);
        }

        public override long Seek(long offset, SeekOrigin origin)
        {
            return _captureStream.Seek(offset, origin);
        }

        public override void SetLength(long value)
        {
            _captureStream.SetLength(value);
        }

        public override void Write(byte[] buffer, int offset, int count)
        {
            _originalBody.Write(buffer, offset, count);
            _captureStream.Write(buffer, offset, count);
        }

        public override async Task WriteAsync(byte[] buffer, int offset, int count, CancellationToken cancellationToken)
        {
            await _originalBody.WriteAsync(buffer, offset, count, cancellationToken);
            await _captureStream.WriteAsync(buffer, offset, count, cancellationToken);
        }

        public override void Close()
        {
            _originalBody.Close();
            _captureStream.Close();
            base.Close();
        }

        protected override void Dispose(bool disposing)
        {
            _originalBody.Dispose();
            _captureStream.Dispose();
            base.Dispose(disposing);
        }
    }
}