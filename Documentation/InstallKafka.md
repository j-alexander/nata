#Kafka Development Environment

## Java Installation
1. Download and install [Java][java] (e.g. `jre1.8.0_91`)
2. In System Properties -> Environment Variables -> System Variables
  * variable `JAVA_HOME` has value `C:\Program Files\Java\jre1.8.0_91`
  * variable `PATH` contains value `C:\Program Files\Java\jre1.8.0_91`
3. Update these values each time you update the JRE
4. Restart Powershell and verify the `java -version`

## Zookeeper Installation
1. Download [Zookeeper][zookeeper] (e.g. `3.4.8`)
2. Download [nssm][nssm] for Windows.
3. Extract both to `C:\Program Files\Zookeeper\{bin,conf,etc}`
4. Change `C:\Program Files\Zookeeper\conf\zoo_sample.cfg` to `zoo.cfg`
  * `dataDir=/ProgramData/Zookeeper`
  * `clientPortBindAddress=127.0.0.1` for a development workstation
5. Change `C:\Program Files\Zookeeper\bin\zkEnv.cmd`
  * `set ZOO_LOG_DIR="C:\ProgramData\Zookeeper\logs"`
  * `set ZOO_LOG4J_PROP=INFO,CONSOLE,ROLLINGFILE`
6. Configure the Zookeeper service:

```powershell
nssm.exe install Zookeeper "C:\Program Files\Zookeeper\bin\zkServer.cmd"
```

## Kafka Installation
1. Download [Kafka][kafka] (e.g. `2.11-0.9.0.1`)
2. Download [nssm][nssm] for Windows.
3. Extract both to `C:\Program Files\Kafka\{bin,config,libs,etc}`
4. Change `C:\Program Files\Kafka\config\server.properties`
  * for a development workstation:
    ```
    host.name=localhost
    zookeeper.connect=127.0.0.1:2181
    ```

  * customize defaults for event sourcing:
    ```
    log.dir=/ProgramData/Kafka/data
    log.retention.hours=-1
    log.retention.bytes=-1
    log.segment.bytes=104857600
    auto.create.topics.enable=true
    delete.topic.enable=true
    ```

5. Change `C:\Program Files\Kafka\config\log4j.properties`
  * `kafka.logs.dir=/ProgramData/Kafka/logs`

6. Change `C:\Program Files\Kafka\bin\windows\kafka-run-class.bat`
  * _before:_
    ```
    pushd %~dp0..\..
    set BASE_DIR=%CD%
    popd
    ```
  * _after:_
    ```
    set BASE_DIR=/progra~1/kafka/
    ```

7. Change `C:\Program Files\Kafka\bin\windows\kafka-server-start.bat`
  * _before:_
    ```
    SetLocal
    set KAFKA_LOG4J_OPTS=-Dlog4j.configuration=file:%~dp0../../config/log4j.properties
    set KAFKA_HEAP_OPTS=-Xmx1G -Xms1G
    %~dp0kafka-run-class.bat kafka.Kafka %*
    EndLocal
    ```
  * _after:_
    ```
    SetLocal
    set KAFKA_LOG4J_OPTS=-Dlog4j.configuration=file:/progra~1/kafka/config/log4j.properties
    set KAFKA_HEAP_OPTS=-Xmx1G -Xms1G
    /progra~1/kafka/bin/windows/kafka-run-class.bat kafka.Kafka %*
    EndLocal
    ```
  * Similar changes can also be made to the following:
    * `kafka-console-consumer.bat`
    * `kafka-console-producer.bat`
    * `kafka-topics.bat`

9. Configure the Kafka service:

```powershell
nssm.exe install Kafka "c:\progra~1\kafka\bin\windows\kafka-server-start.bat" \progra~1\kafka\config\server.properties
```

## Life-cycle
1. Start Zookeeper before Kafka
    ```
    net start zookeeper
    net start kafka
    ```
2. Monitor Log Entries (using `baretail` or `cat`)
    ```
    cat -wait "C:\ProgramData\Zookeeper\logs\zookeeper.log"
    cat -wait "C:\ProgramData\Kafka\logs\controller.log"
    cat -wait "C:\ProgramData\Kafka\logs\kafka-authorizer.log"
    cat -wait "C:\ProgramData\Kafka\logs\kafka-request.log"
    cat -wait "C:\ProgramData\Kafka\logs\log-cleaner.log"
    cat -wait "C:\ProgramData\Kafka\logs\state-change.log"
    cat -wait "C:\ProgramData\Kafka\logs\server.log"
    ```
4. Data is stored in `C:\ProgramData\Kafka\data`
  * Produce events for the broker @ `tcp://127.0.0.1:9092`
  * Consume events from zookeeper @ `127.0.0.1:8081`
5. Stop Kafka before Zookeeper
    ```
    net stop kafka
     net stop zookeeper
    ```
6. Reset Data
    ```
    rm -recurse "C:\ProgramData\Kafka\data\*"
    rm -recurse "C:\ProgramData\Zookeeper\version-2\*"
    ```

  [java]: http://www.oracle.com/technetwork/java/javase/downloads/jre8-downloads-2133155.html "Java Downloads"
  [nssm]: https://nssm.cc "Non-Sucking Service Manager"
  [zookeeper]: https://zookeeper.apache.org/releases.html#download "Apache Zookeeper Downloads"
  [kafka]: http://kafka.apache.org/downloads.html "Apache Kafka Downloads"