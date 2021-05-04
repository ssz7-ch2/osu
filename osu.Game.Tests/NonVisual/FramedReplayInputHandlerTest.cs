// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System;
using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using osu.Game.Replays;
using osu.Game.Rulesets.Replays;

namespace osu.Game.Tests.NonVisual
{
    [TestFixture]
    public class FramedReplayInputHandlerTest
    {
        private Replay replay;
        private TestInputHandler handler;

        [SetUp]
        public void SetUp()
        {
            handler = new TestInputHandler(replay = new Replay
            {
                HasReceivedAllFrames = false
            });
        }

        [Test]
        public void TestNormalPlayback()
        {
            setReplayFrames();

            setTime(0, 0);
            confirmCurrentFrame(0);
            confirmNextFrame(1);

            // if we hit the first frame perfectly, time should progress to it.
            setTime(1000, 1000);
            confirmCurrentFrame(1);
            confirmNextFrame(2);

            // in between non-important frames should progress based on input.
            setTime(1200, 1200);
            confirmCurrentFrame(1);

            setTime(1400, 1400);
            confirmCurrentFrame(1);

            // progressing beyond the next frame should force time to that frame once.
            setTime(2200, 2000);
            confirmCurrentFrame(2);

            // second attempt should progress to input time
            setTime(2200, 2200);
            confirmCurrentFrame(2);

            // entering important section
            setTime(3000, 3000);
            confirmCurrentFrame(3);

            // cannot progress within
            setTime(3500, null);
            confirmCurrentFrame(3);

            setTime(4000, 4000);
            confirmCurrentFrame(4);

            // still cannot progress
            setTime(4500, null);
            confirmCurrentFrame(4);

            setTime(5200, 5000);
            confirmCurrentFrame(5);

            // important section AllowedImportantTimeSpan allowance
            setTime(5200, 5200);
            confirmCurrentFrame(5);

            setTime(7200, 7000);
            confirmCurrentFrame(6);

            setTime(7200, null);
            confirmCurrentFrame(6);

            // exited important section
            setTime(8200, 8000);
            confirmCurrentFrame(7);
            confirmNextFrame(null);

            setTime(8200, 8200);
            confirmCurrentFrame(7);
            confirmNextFrame(null);
        }

        [Test]
        public void TestIntroTime()
        {
            setReplayFrames();

            setTime(-1000, -1000);
            confirmCurrentFrame(null);
            confirmNextFrame(0);

            setTime(-500, -500);
            confirmCurrentFrame(null);
            confirmNextFrame(0);

            setTime(0, 0);
            confirmCurrentFrame(0);
            confirmNextFrame(1);
        }

        [Test]
        public void TestBasicRewind()
        {
            setReplayFrames();

            setTime(2800, 0);
            setTime(2800, 1000);
            setTime(2800, 2000);
            setTime(2800, 2800);
            confirmCurrentFrame(2);
            confirmNextFrame(3);

            // pivot without crossing a frame boundary
            setTime(2700, 2700);
            confirmCurrentFrame(2);
            confirmNextFrame(3);

            // cross current frame boundary
            setTime(1980, 2000);
            confirmCurrentFrame(2);
            confirmNextFrame(3);

            setTime(1200, 1200);
            confirmCurrentFrame(1);
            confirmNextFrame(2);

            // ensure each frame plays out until start
            setTime(-500, 1000);
            confirmCurrentFrame(1);
            confirmNextFrame(2);

            setTime(-500, 0);
            confirmCurrentFrame(0);
            confirmNextFrame(1);

            setTime(-500, -500);
            confirmCurrentFrame(null);
            confirmNextFrame(0);
        }

        [Test]
        public void TestRewindInsideImportantSection()
        {
            setReplayFrames();
            fastForwardToPoint(3000);

            setTime(4000, 4000);
            confirmCurrentFrame(4);
            confirmNextFrame(5);

            setTime(3500, null);
            confirmCurrentFrame(3);
            confirmNextFrame(4);

            setTime(3000, 3000);
            confirmCurrentFrame(3);
            confirmNextFrame(4);

            setTime(3500, null);
            confirmCurrentFrame(3);
            confirmNextFrame(4);

            setTime(4000, 4000);
            confirmCurrentFrame(4);
            confirmNextFrame(5);

            setTime(4500, null);
            confirmCurrentFrame(4);
            confirmNextFrame(5);

            setTime(4000, 4000);
            confirmCurrentFrame(4);
            confirmNextFrame(5);

            setTime(3500, null);
            confirmCurrentFrame(3);
            confirmNextFrame(4);

            setTime(3000, 3000);
            confirmCurrentFrame(3);
            confirmNextFrame(4);
        }

        [Test]
        public void TestRewindOutOfImportantSection()
        {
            setReplayFrames();
            fastForwardToPoint(3500);

            confirmCurrentFrame(3);
            confirmNextFrame(4);

            setTime(3200, null);
            confirmCurrentFrame(3);
            confirmNextFrame(4);

            setTime(3000, 3000);
            confirmCurrentFrame(3);
            confirmNextFrame(4);

            setTime(2800, 2800);
            confirmCurrentFrame(2);
            confirmNextFrame(3);
        }

        [Test]
        public void TestReplayStreaming()
        {
            // no frames are arrived yet
            setTime(0, null);
            setTime(1000, null);
            Assert.IsTrue(handler.WaitingForFrame, "Should be waiting for the first frame");

            replay.Frames.Add(new TestReplayFrame(0));
            replay.Frames.Add(new TestReplayFrame(1000));

            // should always play from beginning
            setTime(1000, 0);
            confirmCurrentFrame(0);
            Assert.IsFalse(handler.WaitingForFrame, "Should not be waiting yet");
            setTime(1000, 1000);
            confirmCurrentFrame(1);
            confirmNextFrame(null);
            Assert.IsTrue(handler.WaitingForFrame, "Should be waiting");

            // cannot seek beyond the last frame
            setTime(1500, null);
            confirmCurrentFrame(1);

            setTime(-100, 0);
            confirmCurrentFrame(0);

            // can seek to the point before the first frame, however
            setTime(-100, -100);
            confirmCurrentFrame(null);
            confirmNextFrame(0);

            fastForwardToPoint(1000);
            setTime(3000, null);
            replay.Frames.Add(new TestReplayFrame(2000));
            confirmCurrentFrame(1);
            setTime(1000, 1000);
            setTime(3000, 2000);
        }

        [Test]
        public void TestMultipleFramesSameTime()
        {
            replay.Frames.Add(new TestReplayFrame(0));
            replay.Frames.Add(new TestReplayFrame(0));
            replay.Frames.Add(new TestReplayFrame(1000));
            replay.Frames.Add(new TestReplayFrame(1000));
            replay.Frames.Add(new TestReplayFrame(2000));

            // forward direction is prioritized when multiple frames have the same time.
            setTime(0, 0);
            setTime(0, 0);

            setTime(2000, 1000);
            setTime(2000, 1000);

            setTime(1000, 1000);
            setTime(1000, 1000);
            setTime(-100, 1000);
            setTime(-100, 0);
            setTime(-100, 0);
            setTime(-100, -100);
        }

        [Test]
        public void TestReplayFramesSortStability()
        {
            const double repeating_time = 5000;

            // add a range of frames randomized in time but have a "data" assigned to them in ascending order.
            replay.Frames.AddRange(new[]
            {
                new TestReplayFrame(repeating_time, true, 0),
                new TestReplayFrame(0, true, 1),
                new TestReplayFrame(3000, true, 2),
                new TestReplayFrame(repeating_time, true, 3),
                new TestReplayFrame(repeating_time, true, 4),
                new TestReplayFrame(6000, true, 5),
                new TestReplayFrame(9000, true, 6),
                new TestReplayFrame(repeating_time, true, 7),
                new TestReplayFrame(repeating_time, true, 8),
                new TestReplayFrame(1000, true, 9),
                new TestReplayFrame(11000, true, 10),
                new TestReplayFrame(21000, true, 11),
                new TestReplayFrame(4000, true, 12),
                new TestReplayFrame(repeating_time, true, 13),
                new TestReplayFrame(repeating_time, true, 14),
                new TestReplayFrame(8000, true, 15),
                new TestReplayFrame(2000, true, 16),
                new TestReplayFrame(7000, true, 17),
                new TestReplayFrame(repeating_time, true, 18),
                new TestReplayFrame(repeating_time, true, 19),
                new TestReplayFrame(10000, true, 20),
            });

            replay.HasReceivedAllFrames = true;

            // create a new handler with the replay for the sort to be performed.
            handler = new TestInputHandler(replay);

            // ensure sort stability by checking whether the "data" assigned to each time-repeated frame is in ascending order, as it was before sort.
            var repeatingTimeFramesData = replay.Frames
                                                .Cast<TestReplayFrame>()
                                                .Where(f => f.Time == repeating_time)
                                                .Select(f => f.Data);

            Assert.That(repeatingTimeFramesData, Is.Ordered.Ascending);
        }

        private void setReplayFrames()
        {
            replay.Frames = new List<ReplayFrame>
            {
                new TestReplayFrame(0),
                new TestReplayFrame(1000),
                new TestReplayFrame(2000),
                new TestReplayFrame(3000, true),
                new TestReplayFrame(4000, true),
                new TestReplayFrame(5000, true),
                new TestReplayFrame(7000, true),
                new TestReplayFrame(8000),
            };
            replay.HasReceivedAllFrames = true;
        }

        private void fastForwardToPoint(double destination)
        {
            for (int i = 0; i < 1000; i++)
            {
                var time = handler.SetFrameFromTime(destination);
                if (time == null || time == destination)
                    return;
            }

            throw new TimeoutException("Seek was never fulfilled");
        }

        private void setTime(double set, double? expect)
        {
            Assert.AreEqual(expect, handler.SetFrameFromTime(set), "Unexpected return value");
        }

        private void confirmCurrentFrame(int? frame)
        {
            Assert.AreEqual(frame is int x ? replay.Frames[x].Time : (double?)null, handler.CurrentFrame?.Time, "Unexpected current frame");
        }

        private void confirmNextFrame(int? frame)
        {
            Assert.AreEqual(frame is int x ? replay.Frames[x].Time : (double?)null, handler.NextFrame?.Time, "Unexpected next frame");
        }

        private class TestReplayFrame : ReplayFrame
        {
            public readonly bool IsImportant;
            public readonly int Data;

            public TestReplayFrame(double time, bool isImportant = false, int data = 0)
                : base(time)
            {
                IsImportant = isImportant;
                Data = data;
            }
        }

        private class TestInputHandler : FramedReplayInputHandler<TestReplayFrame>
        {
            public TestInputHandler(Replay replay)
                : base(replay)
            {
                FrameAccuratePlayback = true;
            }

            protected override double AllowedImportantTimeSpan => 1000;

            protected override bool IsImportant(TestReplayFrame frame) => frame.IsImportant;
        }
    }
}
