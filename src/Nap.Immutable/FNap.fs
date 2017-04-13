﻿namespace Nap

open System
open System.Net
open System.Net.Http
open Nap

module FNap =
    let get = NapClient.Lets.Get
    let post = NapClient.Lets.Post
    let put = NapClient.Lets.Put
    let delete = NapClient.Lets.Delete

    let setBody body (request:INapRequest) = 
        request.IncludeBody body

    let withCookie url cookieName value (request:INapRequest) = 
        request.IncludeCookie url cookieName value

    let withHeader name value (request:INapRequest) =
        request.IncludeHeader name value

    let withQueryParameter name value (request:INapRequest) =
        request.IncludeQueryParameter name value

    let withoutCookie name (request:INapRequest) =
        request.DoNot().IncludeCookie name

    let withoutHeader name (request:INapRequest) =
        request.DoNot().IncludeHeader name

    let withoutQueryParameter name (request:INapRequest) =
        request.DoNot().IncludeQueryParameter name

    let withMetadata (request:INapRequest) =
        request.FillMetadata()

    let withoutMetadata (request:INapRequest) =
        request.DoNot().FillMetadata()

    let useProxy proxy (request:INapRequest) =
        request.Advanced().SetProxy proxy

    let execute<'T> (request:INapRequest) =
        request.Execute<'T>()

    let executeRaw (request:INapRequest) =
        request.Execute()

    let executeRawAsync (request:INapRequest) =
        request.ExecuteAsync()

    let executeAsync<'T> (request:INapRequest) =
        request.ExecuteAsync<'T>()

    /// <summary>
    /// Configure the request using a configuration function.
    /// </summary>
    /// <param name="f">The configuration function that accepts the current configuration and outputs the new configuration.</param>
    /// <param name="request">The request to permute.</param>
    let configure f (request:INapRequest) =
        f (request.Advanced().Configuration) |> (request :?> NapRequest).ApplyConfig
    
    /// <summary>
    /// Retrieve the configuration stored for a given request.
    /// </summary>
    /// <param name="request">The request to retrieve stored configuration for.</param>
    let getConfiguration (request:INapRequest) =
        request.Advanced().Configuration

    /// <summary>
    /// Apply an arbitrary configuration to a request.
    /// Useful for reusable configuration that needs to be applied.
    /// </summary>
    /// <param name="config">The configuration to apply to the request.</param>
    /// <param name="request">The request to permute.</param>
    let applyConfiguration config (request:INapRequest) =
        (request :?> NapRequest).ApplyConfig config
    
    /// <summary>
    /// Update the configuration to use the serializer specified.
    /// </summary>
    /// <param name="serializer">The serializer to use for the request and/or response (provided the serializer supports the given content types).</param>
    /// <param name="request">The request to permute.</param>
    let useSerializer serializer =
        configure (fun c -> c.AddSerializer serializer)
    
    let useSerializerFor contentType serializer =
        configure <| fun c -> c.AddSerializerFor contentType serializer

    let logUsing logger (request:INapRequest) =
        (request :?> NapRequest).ApplyConfig (
            (request :?> NapRequest).Config.SetLogging logger
        )