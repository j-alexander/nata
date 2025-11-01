namespace Nata.IO.AzureStorage

module Emulator =

    module Account =
        
        let name = @"devstoreaccount1"
        let key = @"Eby8vdM02xNOcqFlqUwJPLlmEtlCDXJ1OUzFT50uSRZ6IFsuFq2UVErCz4I6tq/K1SZFPTOtr/KBHBeksoGMGw=="
        let connectionString = "UseDevelopmentStorage=true"



        let dockerConnectionString ip =
            "DefaultEndpointsProtocol=http;" + 
            "BlobEndpoint=http://"+ip+":10000/dockerstorage;" +
            "QueueEndpoint=http://"+ip+":10001/dockerstorage;" +
            "TableEndpoint=http://"+ip+":10002/dockerstorage;" +
            "AccountName=dockerstorage;" +
            "AccountKey=Eby8vdM02xNOcqFlqUwJPLlmEtlCDXJ1OUzFT50uSRZ6IFsuFq2UVErCz4I6tq/K1SZFPTOtr/KBHBeksoGMGw=="
            
        let localConnectionString  =
            dockerConnectionString "127.0.0.1"

        let exampleDockerConnectionString =
            dockerConnectionString "192.168.137.226"
