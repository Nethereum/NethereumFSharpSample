open Nethereum.Web3
open Nethereum.Web3.Accounts.Managed
open Nethereum.Hex.HexTypes
open System
open System.Threading.Tasks
open Nethereum.ABI.FunctionEncoding.Attributes

type Microsoft.FSharp.Control.AsyncBuilder with
  member x.Bind(t:Task<'T>, f:'T -> Async<'R>) : Async<'R>  = 
    async.Bind(Async.AwaitTask t, f)

type MultipliedEvent() = 
    let mutable a = 0

    [<Parameter("uint256", "a", 1, true)>]
    member this.A with get() = a and set(value) = a <- value

[<EntryPoint>]
let main argv =

  let sender = "0x12890d2cce102216644c59daE5baed380d84830c"
  let password = "password"
  let account = ManagedAccount(sender, password)
  let web3 = Web3(account)

  let bin = "0x606060405260405160208060de833981016040528080519060200190919050505b806000600050819055505b5060a68060386000396000f360606040526000357c010000000000000000000000000000000000000000000000000000000090048063c6888fa1146037576035565b005b604b60048080359060200190919050506061565b6040518082815260200191505060405180910390f35b6000817f61aa1562c4ed1a53026a57ad595b672e1b7c648166127b904365b44401821b7960405180905060405180910390a26000600050548202905060a1565b91905056"
  let abi =  @"[{""constant"":false,""inputs"":[{""name"":""a"",""type"":""uint256""}],""name"":""multiply"",""outputs"":[{""name"":""d"",""type"":""uint256""}],""type"":""function""},{""inputs"":[{""name"":""multiplier"",""type"":""uint256""}],""type"":""constructor""},{""anonymous"":false,""inputs"":[{""indexed"":true,""name"":""a"",""type"":""uint256""}],""name"":""Multiplied"",""type"":""event""}]"
  
  let contractReceipt = 
    Async.RunSynchronously(
            async {
                let! receipt =  web3.Eth.DeployContract.SendRequestAndWaitForReceiptAsync(abi, bin, sender, HexBigInteger(Numerics.BigInteger(900000)), null, 7)
                return receipt;
            }
    )

  let contractAddress = contractReceipt.ContractAddress
  let contract = web3.Eth.GetContract(abi, contractAddress)

  printfn "mined contract: %A" contract.Address

  let multiplyFunc = contract.GetFunction("multiply")
  let multiplyEvent = contract.GetEvent("Multiplied")
  let filterAll = 
    Async.RunSynchronously(
            async {
                let! filter = multiplyEvent.CreateFilterAsync()
                return filter;
            }
    )

  printfn "filter id created: %A" filterAll.Value

  let multiplyReceipt = 
     Async.RunSynchronously(
            async {
                let! receipt =
                    multiplyFunc.SendTransactionAndWaitForReceiptAsync(
                    sender,
                    HexBigInteger(Numerics.BigInteger(4700000)),
                    null,
                    null,
                    7)
               return receipt
            }
   )
  
  let decodedLog = multiplyEvent.DecodeAllEventsForEvent<MultipliedEvent>(multiplyReceipt.Logs);

  printfn "receipt log event value of A (multiplied value): %A" (decodedLog.Item(0).Event.A)

  let eventLogs = 
    Async.RunSynchronously(
            async {
               let! logs = multiplyEvent.GetFilterChanges<MultipliedEvent>(filterAll)
               return logs
            }
    )   
 
  printfn "event log event value of A (multiplied value): %A" (eventLogs.Item(0).Event.A)

 

  while true do
    Console.ReadLine() |> ignore

  0 // return an integer exit code