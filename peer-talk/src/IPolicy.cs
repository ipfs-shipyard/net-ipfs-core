﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace PeerTalk
{
    /// <summary>
    ///   A rule that must be enforced.
    /// </summary>
    /// <typeparam name="T">
    ///   The type of object that the rule applies to.
    /// </typeparam>
    interface IPolicy<T>
    {
        /// <summary>
        ///   Determines if the target passes the rule.
        /// </summary>
        /// <param name="target">
        ///   An object to test against the rule.
        /// </param>
        /// <returns>
        ///   <b>true</b> if the <paramref name="target"/> passes the rule;
        ///   otherwise <b>false</b>.
        /// </returns>
        bool IsAllowed(T target);
    }
}
