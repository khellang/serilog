﻿#if FILE_IO

using System;
using System.Collections.Generic;
using System.IO;
using Xunit;
using Serilog.Events;
using Serilog.Sinks.RollingFile;
using Serilog.Tests.Support;

namespace Serilog.Tests.Sinks.RollingFile
{
    public class RollingFileSinkTests
    {
        [Fact]
        public void LogEventsAreEmittedToTheFileNamedAccordingToTheEventTimestamp()
        {
            TestRollingEventSequence(Some.InformationEvent());
        }

        [Fact]
        public void WhenTheDateChangesTheCorrectFileIsWritten()
        {
            var e1 = Some.InformationEvent();
            var e2 = Some.InformationEvent(e1.Timestamp.AddDays(1));
            TestRollingEventSequence(e1, e2);
        }

        [Fact]
        public void WhenRetentionCountIsSetOldFilesAreDeleted()
        {
            LogEvent e1 = Some.InformationEvent(),
                     e2 = Some.InformationEvent(e1.Timestamp.AddDays(1)),
                     e3 = Some.InformationEvent(e2.Timestamp.AddDays(5));

            TestRollingEventSequence(new[] { e1, e2, e3 }, 2,
                files =>
                {
                    Assert.Equal(3, files.Count);
                    Assert.True(!File.Exists(files[0]));
                    Assert.True(File.Exists(files[1]));
                    Assert.True(File.Exists(files[2]));
                });
        }

        [Fact]
        public void IfTheLogFolderDoesNotExistItWillBeCreated()
        {
            var fileName = Some.String() + "-{Date}.txt";
            var temp = Some.TempFolderPath();
            var folder = Path.Combine(temp, Guid.NewGuid().ToString());
            var pathFormat = Path.Combine(folder, fileName);

            ILogger log = null;

            try
            {
                log = new LoggerConfiguration()
                    .WriteTo.RollingFile(pathFormat, retainedFileCountLimit: 3)
                    .CreateLogger();

                log.Write(Some.InformationEvent());

                Assert.True(Directory.Exists(folder));
            }
            finally
            {
                var disposable = (IDisposable)log;
                if (disposable != null) disposable.Dispose();
                Directory.Delete(temp, true);
            }
        }

        static void TestRollingEventSequence(params LogEvent[] events)
        {
            TestRollingEventSequence(events, null, f => {});
        }

        static void TestRollingEventSequence(
            IEnumerable<LogEvent> events,
            int? retainedFiles,
            Action<IList<string>> verifyWritten)
        {
            var fileName = Some.String() + "-{Date}.txt";
            var folder = Some.TempFolderPath();
            var pathFormat = Path.Combine(folder, fileName);

            var log = new LoggerConfiguration()
                .WriteTo.RollingFile(pathFormat, retainedFileCountLimit: retainedFiles)
                .CreateLogger();

            var verified = new List<string>();

            try
            {
                foreach (var @event in events)
                {
                    Clock.SetTestDateTimeNow(@event.Timestamp.DateTime);
                    log.Write(@event);

                    var expected = pathFormat.Replace("{Date}", @event.Timestamp.ToString("yyyyMMdd"));
                    Assert.True(File.Exists(expected));

                    verified.Add(expected);
                }
            }
            finally
            {
                ((IDisposable)log).Dispose();
                verifyWritten(verified);
                Directory.Delete(folder, true);
            }
        }
    }
}
#endif
