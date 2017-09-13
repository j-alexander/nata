namespace Nata.Service

open System
open Nata.Core
open Nata.IO

module Binding =

    let private isRequired reason =
        Option.getValueOrYield(fun () -> reason |> NotSupportedException |> raise)

    let private readerFrom channel =
        Channel.tryReaderFrom channel
        |> isRequired "channel needs to support reading from a specific position"
        
    let private subscriberFrom channel =
        Channel.trySubscriberFrom channel
        |> isRequired "channel needs to support subscribing from a specific position"

    let private competitor channel =
        Channel.tryCompetitor channel
        |> isRequired "channel needs to support competition"

    let private writer channel =
        Channel.tryWriter channel
        |> isRequired "channel needs writer support"

    let private writerTo channel =
        Channel.tryWriterTo channel
        |> isRequired "channel needs support for writing to a specific position"


    // starts a consumer on the current thread
    let start = Consumer.start

    // starts a consumer asynchronously
    let startAsync = Consumer.startAsync
        
    // reset the input checkpoint index and current state
    let reset (state:'State)
              (output:Channel<Consumer<'State,'InputIndex>, 'OutputIndex>)
              (index:'InputIndex) =
        Consumer.reset (writer output) index state

    // converts the output channel of one binding into an input for another
    let asInput (channel:Channel<Consumer<'Input,_>,'InputIndex>) : Channel<'Input,'InputIndex> =
        Channel.mapData (Consumer.state, Codec.NotImplemented) channel
            
    // compete for inputs using the fold pattern
    let fold (fn:'State option->'Input->'State)
             (output:Channel<Consumer<'State,'InputIndex>,'OutputIndex>)
             (input:Channel<'Input,'InputIndex>) =
        Consumer.fold (subscriberFrom input) (competitor output) fn
        
    // compete for inputs and apply a projection
    let map (fn:'Input->'Output)
            (output:Channel<Consumer<'Output,'InputIndex>,'OutputIndex>)
            (input:Channel<'Input,'InputIndex>) =
        Consumer.map (subscriberFrom input) (competitor output) fn

    // compete to fold over two types of input
    let bifold (fn:'State option->Choice<'L,'R>->'State)
               (output:Channel<Consumer<'State,'IndexL option*'IndexR option>,'OutputIndex>)
               ((inputLeft,inputRight):Channel<'L,'IndexL>*Channel<'R,'IndexR>) =
        Consumer.bifold (subscriberFrom inputLeft) (subscriberFrom inputRight) (competitor output) fn

    // compete for two types of inputs and apply a projection
    let bimap (fn:Choice<'L,'R>->'State)
              (output:Channel<Consumer<'State,'IndexL option*'IndexR option>,'OutputIndex>)
              ((inputLeft,inputRight):Channel<'L,'IndexL>*Channel<'R,'IndexR>) =
        Consumer.bimap (subscriberFrom inputLeft) (subscriberFrom inputRight) (competitor output) fn
            
    // compete to fold over many inputs of the same type
    let multifold (fn:'State option->'Input->'State)
                  (output:Channel<Consumer<'State,Map<'SourceId,'InputIndex>>,'OutputIndex>)
                  (inputChannelsByCheckpoint:#seq<'SourceId*Channel<'Input,'InputIndex>>) =
        let subscribersByCheckpoint =
            inputChannelsByCheckpoint
            |> Seq.mapSnd subscriberFrom
            |> Map.ofSeq
        Consumer.multifold subscribersByCheckpoint (competitor output) fn

    // compete for many inputs of the same type and apply a projection
    let multimap (fn:'Input->'Output)
                 (output:Channel<Consumer<'Output,Map<'SourceId,'InputIndex>>,'OutputIndex>)
                 (inputChannelsByCheckpoint:#seq<'SourceId*Channel<'Input,'InputIndex>>) =
        let subscribersByCheckpoint =
            inputChannelsByCheckpoint
            |> Seq.mapSnd subscriberFrom
            |> Map.ofSeq
        Consumer.multimap subscribersByCheckpoint (competitor output) fn

    // compete to partition an input channel across multiple dynamic outputs
    let partition (outputFor:'Input->Channel<Consumer<'Input,'InputIndex>,'OutputIndex>)
                  (checkpoint:Channel<Consumer<unit,'InputIndex>,'OutputIndex>)
                  (input:Channel<'Input,'InputIndex>) =
        let partition = outputFor >> (fun channel -> readerFrom channel, writerTo channel)
        Consumer.partition (subscriberFrom input) (competitor checkpoint) partition
        
    // compete to distribute an input channel across multiple dynamic outputs
    let distribute (outputFor:'Input->List<'Output*Channel<Consumer<'Output,'InputIndex>,'OutputIndex>>)
                   (checkpoint:Channel<Consumer<unit,'InputIndex>,'OutputIndex>)
                   (input:Channel<'Input,'InputIndex>) =
        let distribution = outputFor >> List.map (fun (output,channel) -> readerFrom channel, writerTo channel, output)
        Consumer.distribute (subscriberFrom input) (competitor checkpoint) distribution

    // compete to partition an input channel across multiple dynamic outputs
    let multipartition (id:'SourceId)
                       (outputFor:'Input->Channel<Consumer<'Input,Map<'SourceId,'InputIndex>>,'OutputIndex>)
                       (checkpoint:Channel<Consumer<unit,'InputIndex>,'OutputIndex>)
                       (input:Channel<'Input,'InputIndex>) =
        let partition = outputFor >> (fun channel -> readerFrom channel, writerTo channel)
        Consumer.multipartition (subscriberFrom input) (competitor checkpoint) id partition

    // compete to distribute an input channel across multiple dynamic outputs
    let multidistribute (id:'SourceId)
                        (outputFor:'Input->List<'Output*Channel<Consumer<'Output,Map<'SourceId,'InputIndex>>,'OutputIndex>>)
                        (checkpoint:Channel<Consumer<unit,'InputIndex>,'OutputIndex>)
                        (input:Channel<'Input,'InputIndex>) =
        let distribution = outputFor >> List.map (fun (output,channel) -> readerFrom channel, writerTo channel, output)
        Consumer.multidistribute (subscriberFrom input) (competitor checkpoint) id distribution