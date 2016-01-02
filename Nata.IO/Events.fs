namespace Nata.IO

open System

type Event<'Data,'Metadata> = {
    Type : string
    Stream : string
    Date : DateTime
    Data : 'Data
    Metadata : 'Metadata
}

type Writer<'Data,'Metadata> = Event<'Data,'Metadata> -> unit
type WriterTo<'Data,'Metadata,'Index> = 'Index -> Event<'Data,'Metadata> -> 'Index

type Reader<'Data,'Metadata> = unit -> seq<Event<'Data,'Metadata>>
type ReaderFrom<'Data,'Metadata,'Index> = 'Index -> seq<Event<'Data,'Metadata> * 'Index>

type Subscriber<'Data,'Metadata> = unit -> seq<Event<'Data,'Metadata>>
type SubscriberFrom<'Data,'Metadata,'Index> = 'Index -> seq<Event<'Data,'Metadata> * 'Index>

type Capability<'Data,'Metadata,'Index> =
    | Writer            of Writer<'Data,'Metadata>
    | WriterTo          of WriterTo<'Data,'Metadata,'Index>
    | Reader            of Reader<'Data,'Metadata>
    | ReaderFrom        of ReaderFrom<'Data,'Metadata,'Index>
    | Subscriber        of Subscriber<'Data,'Metadata>
    | SubscriberFrom    of SubscriberFrom<'Data,'Metadata,'Index>

type Source<'Channel,'Data,'Metadata,'Index> = 'Channel -> List<Capability<'Data,'Metadata,'Index>>

type Connector<'Configuration,'Channel,'Data,'Metadata,'Index> = 'Configuration -> Source<'Channel,'Data,'Metadata,'Index>