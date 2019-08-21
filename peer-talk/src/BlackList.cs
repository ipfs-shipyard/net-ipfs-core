﻿using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace PeerTalk
{
    /// <summary>
    ///   A sequence of targets that are not approved.
    /// </summary>
    /// <typeparam name="T">
    ///   The type of object that the rule applies to.
    /// </typeparam>
    /// <remarks>
    ///   Only targets that are not defined will pass.
    /// </remarks>
    public class BlackList<T> : ConcurrentBag<T>, IPolicy<T>
        where T : IEquatable<T>
    {
        /// <inheritdoc />
        public bool IsAllowed(T target)
        {
            return !this.Contains(target);
        }

    }
}
