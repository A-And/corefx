﻿// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Threading;
using System.Threading.Tasks;

namespace System.Net.Http
{
    internal partial class HttpConnection : IDisposable
    {
        private sealed class ContentLengthWriteStream : HttpContentWriteStream
        {
            public ContentLengthWriteStream(HttpConnection connection) : base(connection)
            {
            }

            public override Task WriteAsync(byte[] buffer, int offset, int count, CancellationToken ignored) // token ignored as it comes from SendAsync
            {
                ValidateBufferArgs(buffer, offset, count);
                return WriteAsync(new ReadOnlyMemory<byte>(buffer, offset, count), ignored);
            }

            public override Task WriteAsync(ReadOnlyMemory<byte> source, CancellationToken ignored) // token ignored as it comes from SendAsync
            {
                if (_connection._currentRequest == null)
                {
                    // Avoid sending anything if the response has already completed, in which case there's no point
                    // sending further data (this might happen, for example, on a redirect.)
                    return Task.CompletedTask;
                }

                // Have the connection write the data, skipping the buffer. Importantly, this will
                // force a flush of anything already in the buffer, i.e. any remaining request headers
                // that are still buffered.
                return _connection.WriteWithoutBufferingAsync(source);
            }

            public override Task FlushAsync(CancellationToken ignored) => // token ignored as it comes from SendAsync
                _connection.FlushAsync();

            public override Task FinishAsync()
            {
                _connection = null;
                return Task.CompletedTask;
            }
        }
    }
}
