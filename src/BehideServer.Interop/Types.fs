namespace BehideServer.Types

open System
open Smoosh

type Id = Guid
module Id =
    let CreateOf (x: Id -> _) = Guid.NewGuid() |> x

    let TryParse (str: string) : Id option =
        match Guid.TryParse str with
        | true, id -> Some id
        | false, _ -> None

    let TryParseBytes (bytes: byte []) : Id option =
        try
            new Guid(bytes) |> Some
        with
        | _ -> None

    let ToBytes (id: Id) = id.ToByteArray()

type GuidHelper =
    static member TryParseBytes (bytes: byte [], out: Guid outref) : bool =
        match bytes |> Id.TryParseBytes with
        | Some id -> out <- id; true
        | None -> false

type PlayerId =
    | PlayerId of Id
    static member ToBytes (PlayerId playerId) = playerId |> Id.ToBytes
    member this.ToBytes() = PlayerId.ToBytes this
    static member TryParseBytes bytes = bytes |> Id.TryParseBytes |> Option.map PlayerId
    static member TryParseBytes (bytes, out: PlayerId outref) =
        let playerIdOpt =
            bytes
            |> Id.TryParseBytes
            |> Option.map PlayerId

        match playerIdOpt with
        | Some playerId ->
            out <- playerId
            true
        | None -> false

type RoomId =
    private
    | RoomId of string

    static member private possibilities =
        [| 'A'; 'B'; 'C'; 'D'; 'E'; 'F'; 'G'; 'H'; 'I'; 'J'; 'K'; 'L';
           'M'; 'N'; 'O'; 'P'; 'Q'; 'R'; 'S'; 'T'; 'U'; 'V'; 'W'; 'X';
           'Y'; 'Z'; '0'; '1'; '2'; '3'; '4'; '5'; '6'; '7'; '8'; '9'; |]

    static member private possibilitiesByte =
        [| 0uy; 1uy; 2uy; 3uy; 4uy; 5uy; 6uy; 7uy; 8uy; 9uy; 10uy; 11uy;
           12uy; 13uy; 14uy; 15uy; 16uy; 17uy; 18uy; 19uy; 20uy; 21uy; 22uy; 23uy;
           24uy; 25uy; 26uy; 27uy; 28uy; 29uy; 30uy; 31uy; 32uy; 33uy; 34uy; 35uy; |]

    static member Create() : RoomId =
        [| 0..3 |]
        |> Array.map (fun _ ->
            let charIndex =
                Random().NextDouble()
                * (float <| RoomId.possibilities.Length - 1)
                |> Math.Round
                |> int

            RoomId.possibilities |> Array.item charIndex)
        |> String
        |> RoomId

    override this.ToString() = this |> function RoomId rawRoomId -> rawRoomId.ToUpper()

    static member TryParse(str: string) =
        let str = str.ToUpper()

        str
        |> Seq.forall (fun char -> Array.contains char RoomId.possibilities)
        |> function
            | true -> Some (RoomId str)
            | false -> None

    static member TryParse(str: string, out: RoomId outref) =
        match str |> RoomId.TryParse with
        | Some roomId -> out <- roomId; true
        | None -> false

    static member ToBytes(RoomId id) =
        id.ToCharArray()
        |> Array.map (fun char -> Array.findIndex ((=) char) RoomId.possibilities)
        |> Array.map (fun i -> RoomId.possibilitiesByte.[i])

    static member TryParseBytes(bytes: byte []) =
        bytes
        |> Array.map (fun byte -> Array.tryFindIndex ((=) byte) RoomId.possibilitiesByte)
        |> Array.map (Option.map (fun charIndex -> Array.item charIndex RoomId.possibilities))
        |> fun x ->
            match x |> Array.contains None with
            | true -> None
            | false ->
                x
                |> Array.map Option.get
                |> String
                |> RoomId
                |> Some

    static member TryParseBytes(bytes: byte [], out: RoomId outref) =
        match bytes |> RoomId.TryParseBytes with
        | Some roomId -> out <- roomId; true
        | None -> false

type Player =
    { Id: PlayerId
      IpPort: string
      Username: string
      CurrentRoomId: RoomId option }

type Room =
    { Id: RoomId
      EpicId: Id
      CurrentRound: int
      MaxPlayers: int
      Owner: PlayerId
      Players: Player [] }

    static member private encoder = Encoder.mkEncoder<Room>()
    static member private decoder = Decoder.mkDecoder<Room>()

    static member ToBytes room = Room.encoder room |> Seq.toArray
    member this.ToBytes() = Room.ToBytes this

    static member TryParse (bytes: byte []) =
        try Room.decoder (bytes |> Seq.toArray) |> Some
        with | _ -> None

    static member TryParse (bytes: byte [], out: Room outref) : bool =
        match bytes |> Room.TryParse with
        | Some room -> out <- room; true
        | None -> false