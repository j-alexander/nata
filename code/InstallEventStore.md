#EventStore Development Environment

## Installation
1. Download [EventStore][eventstore] (e.g. `3.6.2.0`)
2. Download [nssm][nssm] for 64-bit Windows (e.g. `2.24`)
3. Install both to `C:\Program Files\EventStore`
4. Configure EventStore service:

```powershell
nssm.exe install EventStore "C:\Program Files\EventStore\EventStore.ClusterNode.exe" 
  --db "C:\ProgramData\EventStore\data" 
  --log "C:\ProgramData\EventStore\logs" 
  --run-projections All
```

## Life-cycle
1. Start EventStore
    ```
    net start eventstore
    ```
2. [Browse](http://localhost:2113/) streams as `admin` with password `changeit`.
3. Monitor Log Entries
    ```
    cat -wait "C:\ProgramData\EventStore\logs\2016-06-04\127.0.0.1-2113-cluster-node.log"
    ```
4. Stop EventStore
    ```
    net stop eventstore
    ```
5. Reset Data
    ```
    rm -recurse "C:\ProgramData\EventStore\data\*"
    ```

  [eventstore]: https://geteventstore.com/downloads/ "EventStore Website" 
  [nssm]: https://nssm.cc "Non-Sucking Service Manager"