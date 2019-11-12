﻿// Copyright © 2019, Silverlake Software LLC.  All Rights Reserved.
// SILVERLAKE SOFTWARE LLC CONFIDENTIAL INFORMATION

// Created by Jamie da Silva on 10/13/2019 3:01 PM

using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Xunit;
using Xunit.Abstractions;

namespace Mailboxes.Tests
{
    public class PriorityMailboxTests : MailboxBaseTests
    {
        public PriorityMailboxTests(ITestOutputHelper output) : base(output) { }

        protected override Mailbox CreateMailbox() => new PriorityMailbox();

        [Fact]
        public void PrioritizesActions()
        {
            var sut = new PriorityMailbox(new TestComparer());
            using var mre = new ManualResetEventSlim();
            using var mre2 = new ManualResetEventSlim();
            bool executedStepA = false;
            bool executedStepB = false;
            sut.Execute(() => { mre.Wait(); });
            sut.Execute(() =>
            {
                executedStepB = true;
                Assert.True(executedStepA);
                mre2.Set();
            }, "b");
            sut.Execute(() =>
            {
                executedStepA = true;
                Assert.False(executedStepB);
            }, "a");

            mre.Set();
            mre2.Wait();

            Assert.True(executedStepB);
        }

        [Fact]
        public void SettingStateViaAwaitWorks()
        {
            var sut = new PriorityMailbox(new TestComparer());
            using var mre = new ManualResetEventSlim();
            using var mre2 = new ManualResetEventSlim();
            using var mre3 = new ManualResetEventSlim();
            using var mre4 = new ManualResetEventSlim();
            bool executedStepA = false;
            bool executedStepB = false;

            var task1 = Task.Run(async () =>
            {
                await sut;
                mre.Set();
                mre2.Wait();
            });

            mre.Wait();

            var task2 = Task.Run(async () =>
            {
                await sut.WithContext("b");
                executedStepB = true;
                Assert.True(executedStepA);
            });

            Thread.Sleep(5);

            var task3 = Task.Run(async () =>
            {
                await sut.WithContext("a");
                executedStepA = true;
                Assert.False(executedStepB);
            });

            Thread.Sleep(5);

            mre2.Set();
            Task.WaitAll(task1, task2, task3);
            Assert.True(executedStepB);
        }

        [Fact]
        public void SettingStateForTaskContinuationWorks()
        {
            var sut = new PriorityMailbox(new TestComparer());
            using var mre = new ManualResetEventSlim();
            using var mre2 = new ManualResetEventSlim();
            using var mre3 = new ManualResetEventSlim();
            using var mre4 = new ManualResetEventSlim();
            bool executedStepA = false;
            bool executedStepB = false;

            var task1 = Task.Run(async () =>
            {
                await sut;
                mre.Set();
                mre2.Wait();
            });

            mre.Wait();

            var task2 = Task.Run(async () =>
            {
                await sut; 
                await Task.Delay(1).ContinueWithContext("b");
                executedStepB = true;
                Assert.True(executedStepA);
            });

            Thread.Sleep(5);

            var task3 = Task.Run(async () =>
            {
                await sut;
                await Task.Delay(1).ContinueWithContext("a");
                executedStepA = true;
                Assert.False(executedStepB);
            });

            Thread.Sleep(5);

            mre2.Set();
            Task.WaitAll(task1, task2, task3);
            Assert.True(executedStepB);
        }

        public class TestComparer : IComparer<object?>
        {
            public int Compare(object? x, object? y) => string.CompareOrdinal(x as string, y as string);
        }

    }
}