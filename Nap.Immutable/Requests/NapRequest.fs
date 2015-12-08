﻿namespace Nap

open System
open System.Collections.Generic
open System.IO
open System.Net
open System.Net.Http
open System.Reflection
open System.Threading.Tasks

open Nap.Serializers.Base

open Finalizable
open AsyncFinalizable
open Operators
open Text

type InternalResponse =
    {
        Raw : HttpResponseMessage
        Content : string
        Deserialized : obj option
    }

type NapRequest =
    {
        Config          : NapConfig
        Response        : InternalResponse option
        RequestEvents   : Map<string, Event<NapRequest> list>
        Url             : string
        Method          : HttpMethod
        QueryParameters : Map<string, string>
        Headers         : Map<string, string>
        Cookies         : (Uri*Cookie) list
        Content         : string
        ClientCreator   : INapRequest -> HttpClient
    }
    with
    (*** Properties ***)
    static member private CreateClient (irequest:INapRequest) = 
        match irequest with
        | :? NapRequest as request ->
            let handler = new HttpClientHandler()
            let request = downcast irequest
            match request.Config.Advanced.Proxy with
            | Some(proxy) -> () // TODO: Add proxy
            | None -> ()
            for (uri,cookie) in request.Cookies do
                handler.CookieContainer.Add(uri, cookie)
            new HttpClient(handler)
        | _ ->
            new HttpClient()

    static member internal Default =
        {
            Config = NapConfig.Default
            Response = None
            RequestEvents = Map.empty
            Url = ""
            Method = HttpMethod.Get
            QueryParameters = Map.empty
            Headers = Map.empty
            Cookies = List.empty
            Content = ""
            ClientCreator = NapRequest.CreateClient
        }

    (*** Config helpers ***)
    member x.ApplyConfig config =
        x.Verbose "NapRequest.ApplyConfig()" <| sprintf "Applying configuration %A" config
        Continuing(x)
        |> NapRequest.RunEvents "BeforeConfigurationApplied"
        <!> fun request ->
            { request with
                Config = config
                Url = config.BaseUri
                QueryParameters = request.Config.QueryParameters |> Map.join config.QueryParameters
                Headers = request.Config.Headers |> Map.join config.Headers
                ClientCreator = 
                    match config.Advanced.ClientCreator with
                    | Some(creator) -> fun nr -> creator (box nr)
                    | None -> NapRequest.CreateClient
            }
        |> NapRequest.RunEvents "AfterConfigurationApplied"
        |> Finalizable.get

    (*** Constructors ***)
    static member Create () =
        NapRequest.Default
        |> Continuing
        |> NapRequest.RunEvents "CreatingNapRequest"
        |> Finalizable.get
        |> fun x -> x :> INapRequest
    static member Create config =
        NapRequest.Default
        |> Continuing
        |> NapRequest.RunEvents "CreatingNapRequest"
        |> Finalizable.get
        |> fun x -> x.ApplyConfig(config)
        |> fun x -> x :> INapRequest
    static member Create (config,uri) =
        NapRequest.Default
        |> Continuing
        |> NapRequest.RunEvents "CreatingNapRequest"
        <!> fun x -> x.ApplyConfig(config)
        |> Finalizable.get
        |> fun x -> { x with Url = uri }
        |> fun x -> x :> INapRequest
    static member Create (config,uri,httpMethod) =
        NapRequest.Default
        |> Continuing
        |> NapRequest.RunEvents "CreatingNapRequest"
        <!> fun x -> x.ApplyConfig(config)
        |> Finalizable.get
        |> fun x -> { x with Url = uri; Method = httpMethod}
        |> fun x -> x :> INapRequest

    (*** Event helpers ***)
    static member internal RunEventsAsync eventName asyncFinalizableRequest =
        async {
            let! finalizableRequest = asyncFinalizableRequest
            return NapRequest.RunEvents eventName finalizableRequest
        }

    static member internal RunEvents eventName (finalizableRequest:FinalizableType<NapRequest, _>) =
        match finalizableRequest with
        | Continuing(request) ->
            let result =
                match request.RequestEvents |> Map.tryFind eventName with
                | Some(event) ->
                    request.Debug "Nap.NapRequest.RunEvents()" (sprintf "Running events for %s on %A" eventName request)
                    event |> List.fold (fun r event -> r >>= event.RunEvent) (Continuing(request))
                | None -> finalizableRequest
            if eventName = "*" then result else result |> NapRequest.RunEvents "*"
        | Final(_) -> finalizableRequest
        
    (*** Logging helpers ***)
    member private x.Log logLevel path message =
        x.Config.Log logLevel path message
    member private x.Verbose = x.Log LogLevel.Verbose
    member private x.Debug path message = x.Log LogLevel.Debug path message
    member private x.Info = x.Log LogLevel.Info
    member private x.Warn = x.Log LogLevel.Warn
    member private x.Error = x.Log LogLevel.Error
    member private x.Fatal path message = x.Log LogLevel.Fatal path message; failwith (sprintf "[%s] %s" path message)

    (*** Request running implementation ***)
    member private x.CreateUrl () =
        let pairToQueryStringValue (k,v) =
            sprintf "%s=%s" k <| WebUtility.UrlEncode(v)
        x.Url |?? String.Empty
        |> fun url ->
            match url with
            | Prefix "http" _ -> url
            | _ -> new Uri(new Uri(x.Config.BaseUri), url) |> string
        |> fun url ->
            match x.QueryParameters |> Map.toArray with 
            | [||] -> url
            | parameters -> sprintf "%s?%s" (url.TrimEnd('?')) <|
                                (parameters |> Array.map pairToQueryStringValue |> fun l -> String.Join("&", l))

    member private x.GetSerializer contentType =
        let serializer = x.Config.Serializers |> Map.tryFind contentType
        let seperator = "/"
        match serializer with
        | Some(s) -> s |> Some
        | None -> 
            match contentType with
            | Split seperator (_::tail)  -> x.GetSerializer (tail |> String.concat seperator)
            | _ -> None

    static member private RunRequestAsync request =
        async {
            let excludedHeaders = ["content-type"] |> Set.ofList
            use client = request.ClientCreator (upcast request)
            let content = new StringContent(request.Content |?? "")
            let requestMessage = new HttpRequestMessage(request.Method, request.CreateUrl())
            let allowedDefaultHeaders = request.Headers |> Map.filter (fun headerName _ -> excludedHeaders |> Set.contains(headerName.ToLower()) |> not)
            for (h,v) in allowedDefaultHeaders |> Map.toSeq do
                client.DefaultRequestHeaders.Add(h, v)
            match request.Headers |> Map.toSeq |> Seq.tryFind (fun (k,_) -> k.Equals("content-type", StringComparison.OrdinalIgnoreCase)) with
            | Some((_,v)) -> content.Headers.ContentType.MediaType <- v
            | None -> content.Headers.ContentType.MediaType <- (request.Config.Serializers.[request.Config.Serialization]).ContentType
            requestMessage.Content <- content
            let! response = client.SendAsync(requestMessage) |> Async.AwaitTask
            let! content = response.Content.ReadAsStringAsync() |> Async.AwaitTask
            return { request with Response = { Raw = response; Content = content; Deserialized = None } |> Some }
        }

    static member private Deserialize<'T> request =
        let path = sprintf "NapRequest.RunReqest()/NapRequest.Deserialize<%s>() [%s]" typeof<'T>.FullName request.Url
        match request.Response with
            | Some(response) ->
                let serializerOption = request.GetSerializer <| response.Raw.Content.Headers.ContentType.MediaType
                match serializerOption with
                | Some(serializer) -> 
                    request.Verbose path <| sprintf "Deserializing content with %s deserializer" (serializer.GetType().FullName)
                    { request with
                        Response =
                        {
                            response with Deserialized =
                                (serializer.Deserialize<'T> response.Content)
                                |> fun o -> 
                                        match o with
                                        | None -> 
                                            let parameterlessConstructor =
                                                typeof<'T>.GetTypeInfo().DeclaredConstructors
                                                |> Seq.tryFind (fun c -> c.GetParameters().Length = 0 && not <| c.ContainsGenericParameters)
                                            parameterlessConstructor |> Option.bind (fun c -> c.Invoke(Array.empty) :?> 'T |> Some)
                                        | x -> x
                                |> Option.bind (box>>Some)
                        } |> Some
                    }
                | None ->
                    request.Warn path <| sprintf "No deserializer found for content type '%s'" response.Raw.Content.Headers.ContentType.MediaType
                    request
            | None ->
                request.Warn path "No response recieved."
                request

    static member private FillMetadata<'T> request =
        match request.Config.FillMetadata, request.Response, request.Response |> Option.bind(fun r -> r.Deserialized) with
        | true,Some(response),Some(deserializedObj) ->
            let deserialized = unbox<'T> deserializedObj
            let statusCodeProperty = typeof<'T>.GetRuntimeProperty("StatusCode")
            if isNull statusCodeProperty |> not then
                statusCodeProperty.SetValue(deserialized, Convert.ChangeType(response.Raw.StatusCode, statusCodeProperty.PropertyType))
            let cookieProperty = typeof<'T>.GetRuntimeProperty("Cookies")
            if isNull cookieProperty |> not then
                let cookies =
                    response.Raw.Headers
                    |> Seq.filter (fun h -> h.Key.StartsWith("set-cookie", StringComparison.OrdinalIgnoreCase))
                    |> Seq.filter (fun c -> (c.Value |> Seq.length) > 0)
                    |> Seq.map (fun c -> new KeyValuePair<string, string>(c.Key, c.Value |> Seq.head))
                cookieProperty.SetValue(deserialized, cookies)
            { request with Response = { response with Deserialized = Some(box deserialized) } |> Some } 
        | _ -> request

    member x.ExecuteFSTypedAsync<'T> () =
        async {
            let path = sprintf "NapRequest.Execute() [%s]" x.Url
            x.Info path <| sprintf "Executing %O request with body %s" x.Method x.Content
            x.Verbose path <| sprintf "Executing %O request with parameters full structure %A" x.Method x
            let! processedRequest =
                async.Return <| Continuing(x)
                |> NapRequest.RunEventsAsync "BeforeNapRequestExecution"
                |> AsyncFinalizable.bindAsync (NapRequest.RunRequestAsync)
                |> NapRequest.RunEventsAsync "AfterNapRequestExecution"
                |> NapRequest.RunEventsAsync "BeforeResponseDeserialization"
                <!!> fun response -> NapRequest.Deserialize<'T> response
                |> NapRequest.RunEventsAsync "AfterResponseDeserialization"
                |> AsyncFinalizable.get
                |> Async.map (fun request -> NapRequest.FillMetadata<'T> request)
            let deserializedObj = processedRequest.Response |> Option.bind (fun response -> response.Deserialized)
            return
                match deserializedObj with
                | Some(deserialized) -> unbox<'T> deserialized |> Some
                | None -> None
        }
        
            
    member x.ExecuteFSAsync () = 
        async {
            let path = sprintf "NapRequest.Execute() [%s]" x.Url
            x.Info path <| sprintf "Executing %O request with body %s" x.Method x.Content
            x.Verbose path <| sprintf "Executing %O request with parameters full structure %A" x.Method x
            let! processedRequest =
                async.Return <| Continuing(x)
                |> NapRequest.RunEventsAsync "BeforeNapRequestExecution"
                |> AsyncFinalizable.bindAsync (NapRequest.RunRequestAsync)
                |> NapRequest.RunEventsAsync "AfterNapRequestExecution"
                |> AsyncFinalizable.get
            return processedRequest.Response |> Option.bind (fun response -> response.Content |> Some)
        }

    (*** INapRequest interface implementation ***)
    interface INapRequest with
        member x.Advanced(): IAdvancedNapRequestComponent = 
            x :> IAdvancedNapRequestComponent

        member x.DoNot(): IRemovableNapRequestComponent = 
            x :> IRemovableNapRequestComponent

        member x.Execute(): 'T = 
            let result = x.ExecuteFSTypedAsync () |> Async.RunSynchronously
            defaultArg result Unchecked.defaultof<'T>

        member x.Execute(): string = 
            let result = x.ExecuteFSAsync () |> Async.RunSynchronously
            defaultArg result null

        member x.ExecuteAsync<'T> () : Task<'T> =
            async {
                let! result = x.ExecuteFSTypedAsync<'T> ()
                return defaultArg result Unchecked.defaultof<'T>
            } |> Async.StartAsTask

        member x.ExecuteAsync(): Task<string> = 
            async {
                let! result = x.ExecuteFSTypedAsync<'T> ()
                return defaultArg result null
            } |> Async.StartAsTask

        member x.FillMetadata(): INapRequest = 
            x.Verbose "Nap.NapRequest.FillMetadata()" "Flagging metadata to be filled."
            upcast { x with Config = { x.Config with FillMetadata = true } }

        member x.IncludeBody(body: obj): INapRequest = 
            if body |> isNull then
                x.Verbose "Nap.NapRequest.IncludeBody()" (sprintf "Null object found; serializing nothing.")
                x :> INapRequest
            else
                x.Verbose "Nap.NapRequest.Includebody()" (sprintf "Serializing %O" body)
                x.Debug "Nap.NapRequest.Includebody()" (sprintf "Serializing object of type %s" (body.GetType().FullName))
                Continuing(x)
                |> NapRequest.RunEvents "BeforeRequestSerialization"
                <!> fun request ->
                    match request.Config.Serializers |> Map.tryFind request.Config.Serialization with
                    | Some(serializer) -> { request with Content = serializer.Serialize(body) }
                    | None -> 
                        x.Error "Nap.NapRequest.IncludeBody()" (sprintf "Could not serialize content of type %s with serializer %s" (body.GetType().ToString()) x.Config.Serialization)
                        request
                |> NapRequest.RunEvents "AfterRequestSerialization"
                |> fun r -> upcast Finalizable.get r
        member x.IncludeCookie(url: string) (cookieName: string) (value: string): INapRequest = 
            x.Verbose "Nap.NapRequest.IncludeCookie()" (sprintf "Adding cookie for URL \"%s\". %s: %s" url cookieName value)
            let uri = new Uri(url)
            upcast { x with Cookies = x.Cookies |> List.append ([uri, new Cookie(cookieName, value)]) }
        member x.IncludeHeader(name: string) (value: string): INapRequest = 
            x.Verbose "Nap.NapRequest.IncludeCookie()" (sprintf "Adding header: %s: %s" name value)
            upcast { x with Headers = x.Headers |> Map.add name value }
        member x.IncludeQueryParameter(name: string) (value: string): INapRequest = 
            x.Verbose "Nap.NapRequest.IncludeCookie()" (sprintf "Adding query parameter: %s=%s" name value)
            upcast { x with QueryParameters = x.QueryParameters |> Map.add name value }

    interface IAdvancedNapRequestComponent with
        member x.Configuration : NapConfig = 
            x.Config
        member x.Authentication: AuthenticationNapConfig = 
            x.Config.Advanced.Authentication
        member x.Proxy =
            x.Config.Advanced.Proxy
        member x.ClientCreator = 
            x.ClientCreator
        member x.SetClientCreator clientCreator =
            upcast { x with ClientCreator = (fun nr -> clientCreator.Invoke nr) }
        member x.SetAuthentication(authenticationScheme: AuthenticationNapConfig): INapRequest = 
            x.Debug "Nap.NapRequest.SetAuthentication()" (sprintf "Setting authentication as %A" authenticationScheme)
            Continuing(x)
            |> NapRequest.RunEvents "SetAuthentication"
            |> Finalizable.get
            |> fun r ->
                upcast { r with Config = { r.Config with Advanced = { r.Config.Advanced with Authentication = authenticationScheme }}}
        member x.SetProxy(proxy: ProxyNapConfig): INapRequest = 
            x.Debug "Nap.NapRequest.SetProxy()" (sprintf "Setting proxy as %A" proxy)
            upcast { x with Config = { x.Config with Advanced = { x.Config.Advanced with Proxy = proxy |> Some }}}
        
        
    interface IRemovableNapRequestComponent with
        member x.FillMetadata(): INapRequest = 
            { x with Config = {x.Config with FillMetadata = false } } :> INapRequest
        member x.IncludeBody(): INapRequest = 
            upcast { x with Content = "" }
        member x.IncludeCookie(cookieName: string): INapRequest = 
            upcast { x with Cookies = x.Cookies |> List.filter (fun (_,c) -> c.Name <> cookieName) }
        member x.IncludeHeader(headerName: string): INapRequest = 
            upcast { x with Headers = x.Headers |> Map.remove headerName }
        member x.IncludeQueryParameter(queryParameterName: string): INapRequest = 
            upcast { x with QueryParameters = x.QueryParameters |> Map.remove queryParameterName }

    override x.ToString() =
        sprintf "%A" x
