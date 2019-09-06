﻿using McMaster.Extensions.CommandLineUtils;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Text;
using System.Threading.Tasks;

namespace Ipfs.Cli
{
    [Command(Description = "Manage swapped blocks")]
    [Subcommand("wantlist", typeof(BitswapWantListCommand))]
    [Subcommand("unwant", typeof(BitswapUnwantCommand))]
    [Subcommand("ledger", typeof(BitswapLedgerCommand))]
    [Subcommand("stat", typeof(BitswapStatCommand))]
    class BitswapCommand : CommandBase
    {
        public Program Parent { get; set; }

        protected override Task<int> OnExecute(CommandLineApplication app)
        {
            app.ShowHelp();
            return Task.FromResult(0);
        }
    }

    [Command(Description = "Show blocks currently on the wantlist")]
    class BitswapWantListCommand : CommandBase
    {
        [Option("-p|--peer", Description = "Peer to show wantlist for. Default: self.")]
        public string PeerId { get; set; }

        BitswapCommand Parent { get; set; }

        protected override async Task<int> OnExecute(CommandLineApplication app)
        {
            var Program = Parent.Parent;
            var peer = PeerId == null
                ? null
                : new MultiHash(PeerId);
            var cids = await Program.CoreApi.Bitswap.WantsAsync(peer);
            return Program.Output(app, cids, (data, writer) =>
            {
                foreach (var cid in data)
                {
                    writer.WriteLine(cid.Encode());
                }
            });
        }
    }

    [Command(Description = "Remove a block from the wantlist")]
    class BitswapUnwantCommand : CommandBase
    {
        [Argument(0, "cid", "The content ID of the block")]
        [Required]
        public string Cid { get; set; }

        BitswapCommand Parent { get; set; }

        protected override async Task<int> OnExecute(CommandLineApplication app)
        {
            var Program = Parent.Parent;
            await Program.CoreApi.Bitswap.UnwantAsync(Cid);
            return 0;
        }
    }

    [Command(Description = "Show the current ledger for a peer")]
    class BitswapLedgerCommand : CommandBase
    {
        [Argument(0, "peerid", "The PeerID (B58) of the ledger to inspect")]
        [Required]
        public string PeerId { get; set; }

        BitswapCommand Parent { get; set; }

        protected override async Task<int> OnExecute(CommandLineApplication app)
        {
            var Program = Parent.Parent;
            var peer = new Peer { Id = PeerId };
            var ledger = await Program.CoreApi.Bitswap.LedgerAsync(peer);
            return Program.Output(app, ledger, null);
        }
    }

    [Command(Description = "Show bitswap information")]
    class BitswapStatCommand : CommandBase
    {
        BitswapCommand Parent { get; set; }

        protected override async Task<int> OnExecute(CommandLineApplication app)
        {
            var Program = Parent.Parent;

            var stats = await Program.CoreApi.Stats.BitswapAsync();
            return Program.Output(app, stats, null);
        }
    }

}
