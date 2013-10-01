// OUYA Development Kit C# bindings - Copyright (C) Konaju Games
// This file is subject to the terms and conditions defined in
// file 'LICENSE.txt' which is part of this source code package.

using System;
using System.Threading;
using System.Threading.Tasks;

namespace Ouya.Console.Api
{
    static class TaskExtensions
    {
        /// <summary>
        /// Starts a task that will complete within the timeout period, or fail with a TimeoutException if the timeout is exceeded.
        /// </summary>
        /// <param name="millisecondsTimeout">The timeout in milliseconds</param>
        /// <returns>The task to wait on</returns>
        public static async Task TimeoutAfter(this Task task, int millisecondsTimeout)
        {
            // Short-circuit #1: infinite timeout or task already completed
            if (task.IsCompleted || (millisecondsTimeout == Timeout.Infinite))
            {
                // Either the task has already completed or timeout will never occur.
                // No proxy necessary.
                await task;
                return;
            }

            // Short-circuit #2: zero timeout
            if (millisecondsTimeout == 0)
            {
                // We've already timed out.
                throw new TimeoutException();
            }

            var source = new CancellationTokenSource();
            var token = source.Token;
            var delayTask = Task.Delay(millisecondsTimeout, token);
            if (delayTask == await Task.WhenAny(task, delayTask))
            {
                throw new TimeoutException();
            }
            source.Cancel();
            await task;
        }

        /// <summary>
        /// Starts a task that will complete within the timeout period, or fail with a TimeoutException if the timeout is exceeded.
        /// </summary>
        /// <typeparam name="T">The return type of the task</typeparam>
        /// <param name="millisecondsTimeout">The timeout in milliseconds</param>
        /// <returns>The task to wait on</returns>
        public static async Task<T> TimeoutAfter<T>(this Task<T> task, int millisecondsTimeout)
        {
            // Short-circuit #1: infinite timeout or task already completed
            if (task.IsCompleted || (millisecondsTimeout == Timeout.Infinite))
            {
                // Either the task has already completed or timeout will never occur.
                // No proxy necessary.
                return await task;
            }

            // Short-circuit #2: zero timeout
            if (millisecondsTimeout == 0)
            {
                // We've already timed out.
                throw new TimeoutException();
            }

            var source = new CancellationTokenSource();
            var token = source.Token;
            var delayTask = Task.Delay(millisecondsTimeout, token);
            if (delayTask == await Task.WhenAny(task, delayTask))
            {
                throw new TimeoutException();
            }
            source.Cancel();
            return await task;
        }
    }
}