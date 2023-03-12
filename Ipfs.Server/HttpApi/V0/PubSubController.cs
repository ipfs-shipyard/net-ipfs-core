using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Ipfs.CoreApi;
using Microsoft.AspNetCore.Mvc;

namespace Ipfs.Server.HttpApi.V0;

/// <summary>
///     A list of topics.
/// </summary>
public class PubsubTopicsDto
{
    /// <summary>
    ///     A list of topics.
    /// </summary>
    public IEnumerable<string> Strings;
}

/// <summary>
///     A list of peers.
/// </summary>
public class PubsubPeersDto
{
    /// <summary>
    ///     A list of peer IDs.
    /// </summary>
    public IEnumerable<string> Strings;
}

/// <summary>
///     A published message.
/// </summary>
public class MessageDto
{
    /// <summary>
    ///     The base-64 encoding of the message data.
    /// </summary>
    public string Data;

    /// <summary>
    ///     The base-64 encoding of the author's peer id.
    /// </summary>
    public string From;

    /// <summary>
    ///     The base-64 encoding of the author's unique sequence number.
    /// </summary>
    public string Seqno;

    /// <summary>
    ///     The topics associated with the message.
    /// </summary>
    public string[] TopicIDs;

    /// <summary>
    ///     Create a new instance of the <see cref="MessageDto" />
    ///     from the <see cref="IPublishedMessage" />.
    /// </summary>
    /// <param name="msg">
    ///     A pubsub messagee.
    /// </param>
    public MessageDto(IPublishedMessage msg)
    {
        From = Convert.ToBase64String(msg.Sender.Id.ToArray());
        Seqno = Convert.ToBase64String(msg.SequenceNumber);
        Data = Convert.ToBase64String(msg.DataBytes);
        TopicIDs = msg.Topics.ToArray();
    }
}

/// <summary>
///     Publishing and subscribing to messages on a topic.
/// </summary>
public class PubSubController : IpfsController
{
    /// <summary>
    ///     Creates a new controller.
    /// </summary>
    public PubSubController(ICoreApi ipfs) : base(ipfs)
    {
    }

    /// <summary>
    ///     List all the subscribed topics.
    /// </summary>
    [HttpGet]
    [HttpPost]
    [Route("pubsub/ls")]
    public async Task<PubsubTopicsDto> List()
    {
        return new()
        {
            Strings = await IpfsCore.PubSub.SubscribedTopicsAsync(Cancel)
        };
    }

    /// <summary>
    ///     List all the peers associated with the topic.
    /// </summary>
    /// <param name="arg">
    ///     The topic name or null/empty for "all topics".
    /// </param>
    [HttpGet]
    [HttpPost]
    [Route("pubsub/peers")]
    public async Task<PubsubPeersDto> Peers(string arg)
    {
        var topic = string.IsNullOrEmpty(arg) ? null : arg;
        var peers = await IpfsCore.PubSub.PeersAsync(topic, Cancel);
        return new()
        {
            Strings = peers.Select(p => p.Id.ToString())
        };
    }

    /// <summary>
    ///     Publish a message to a topic.
    /// </summary>
    /// <param name="arg">
    ///     The first arg is the topic name and second is the message.
    /// </param>
    [HttpGet]
    [HttpPost]
    [Route("pubsub/pub")]
    public async Task Publish(string[] arg)
    {
        if (arg.Length != 2)
        {
            throw new ArgumentException("Missing topic and/or message.");
        }

        var message = arg[1].Select(c => (byte)c).ToArray();
        await IpfsCore.PubSub.PublishAsync(arg[0], message, Cancel);
    }

    /// <summary>
    ///     Subscribe to messages on the topic.
    /// </summary>
    /// <param name="arg">
    ///     The topic name.
    /// </param>
    [HttpGet]
    [HttpPost]
    [Route("pubsub/sub")]
    public async Task Subscribe(string arg)
    {
        await IpfsCore.PubSub.SubscribeAsync(arg, message =>
        {
            // Send the published message to the caller.
            var dto = new MessageDto(message);
            StreamJson(dto);
        }, Cancel);

        // Send 200 OK to caller; but do not close the stream
        // so that published messages can be sent.
        Response.ContentType = "application/json";
        Response.StatusCode = 200;
        await Response.Body.FlushAsync();

        // Wait for the caller to cancel.
        try
        {
            await Task.Delay(-1, Cancel);
        }
        catch (TaskCanceledException)
        {
            // eat
        }
    }
}