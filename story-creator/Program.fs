open System

open System.Net.Http
open System.Text
open System.Net.Http.Headers
open System.Text.Json

// story type 
type Genre = | Fantasy | Mystery | SciFi | Romance | Comedy

type Stage =
    | Introduction       // set scene, introduce characters
    | ActionConflict     // main challenge, rising tension
    | AmbiguityMystery   // unpredictable paths, hints, twists
    | ClimaxResolution   // peak conflict and conclusion
    | End                // story finished

// state for the story
type StoryState = {
    Genre: Genre
    CurrentStage: Stage
    History: string list 
    CurrentText: string  
    Choices: string list 
}

module StoryEngine =
    
    let callAiApi (prompt: string) : string * string list =

        use client = new HttpClient()
        let envKey = Environment.GetEnvironmentVariable("AI_API_KEY")
        let apiKey =
            match String.IsNullOrEmpty(envKey) with
            | true -> "sk-123"
            | _ -> envKey
        
        client.DefaultRequestHeaders.Accept.Add(MediaTypeWithQualityHeaderValue("application/json"))
        client.DefaultRequestHeaders.Authorization <- AuthenticationHeaderValue("Bearer", apiKey)
        
        let requestContent = 
            JsonSerializer.Serialize(
                {|
                    model = "llama-3.1-8b-instant"
                    messages = 
                        [|
                            {| role = "system"; content = "You are an interactive storytelling AI. Create engaging, concise story segments with 2 distinct choices for the user." |}
                            {| role = "user"; content = prompt |}
                        |]
                    max_tokens = 300
                    temperature = 0.7
                |})
        let content = new StringContent(requestContent, Encoding.UTF8, "application/json")
        
        try
            let response = client.PostAsync("https://api.groq.com/openai/v1/chat/completions", content).Result
            response.EnsureSuccessStatusCode() |> ignore
            
            let responseBody = response.Content.ReadAsStringAsync().Result
            let jsonResponse = JsonDocument.Parse(responseBody)
            
            let responseText = 
                jsonResponse.RootElement
                    .GetProperty("choices").[0]
                    .GetProperty("message")
                    .GetProperty("content")
                    .GetString()
            

            // expected format: story segment followed by numbered choices
            let lines = responseText.Split([|"\n"|], StringSplitOptions.None)
            
            let storyLines = 
                lines 
                |> Array.takeWhile (fun line -> not(line.Trim().StartsWith("1.") || line.Trim().StartsWith("Option 1:") || String.IsNullOrWhiteSpace(line)))
            
            let choiceLines =
                lines
                |> Array.skipWhile (fun line -> not(line.Trim().StartsWith("1.") || line.Trim().StartsWith("Option 1:") || String.IsNullOrWhiteSpace(line)))
                |> Array.filter (fun line -> 
                    line.Trim().StartsWith("1.") || line.Trim().StartsWith("2.") ||
                    line.Trim().StartsWith("Option 1:") || line.Trim().StartsWith("Option 2:"))
            
            let storySegment = String.Join(" ", storyLines).Trim()
            
            let choices = 
                choiceLines
                |> Array.map (fun line -> 
                    line.Replace("1.", "").Replace("2.", "")
                        .Replace("Option 1:", "").Replace("Option 2:", "")
                        .Trim())
                |> Array.toList
            
            let finalChoices = 
                if List.length choices >= 2 then choices |> List.take 2
                else ["Continue forward"; "Take another path"]
            
            (storySegment, finalChoices)
        with
        | ex -> 
            printfn $"API Error: %s{ex.Message}"
            // Fallback content in case of API failure
            ("The path ahead seems uncertain as you consider your next move.", 
             ["Proceed with caution"; "Try a different approach"])

    // Helper to determine the next logical stage
    let getNextStage current =
        match current with
        | Introduction -> ActionConflict
        | ActionConflict -> AmbiguityMystery
        | AmbiguityMystery -> ClimaxResolution
        | ClimaxResolution -> End
        | End -> End

    let generatePrompt (state: StoryState) (userChoice: string option) : string =
        let stageGoal =
             match state.CurrentStage with
             | ActionConflict -> "Focus on rising action and conflict."
             | AmbiguityMystery -> "Introduce an element of ambiguity or mystery."
             | ClimaxResolution -> "Build towards the climax and resolution."
             | End -> "Provide a concluding paragraph and resolution. Offer no choices."
             | Introduction -> "Start the story with an introduction."

        let context =
            match state.History |> List.tryLast with
            | Some _ -> sprintf "Previous Event: %s" (String.concat " " state.History)
            | None -> "This is the beginning of the story."

        let choiceInfo =
            userChoice
            |> Option.map (sprintf "User chose: %s")
            |> Option.defaultValue ""
            
        let exampleOutput =
            "Example Output Structure:\n" +
            "As Captain {Whatevername}'s ship, the Celestial Horizon, descended onto ...\n" +
            "Option 1: You go with the captin.\n" +
            "Option 2: You let them leave without you.\n"

        $"Generate the next part of a %A{state.Genre} story.\n%s{context}\n%s{choiceInfo}\nCurrent Narrative Goal: %s{stageGoal}\nKeep the story segment concise (1-2 sentences).\nProvide 2 short, distinct choices for actions the user might be able to do based on the segment.\n---\n%s{exampleOutput}"
    // story engine core - pure transition function (conceptually)
    let transition (currentState: StoryState) (userChoice: string) : StoryState =
        if currentState.CurrentStage = End then currentState 
        else
            let prompt = generatePrompt {currentState with CurrentStage = getNextStage currentState.CurrentStage} (Some userChoice)
            let nextText, nextChoices = callAiApi prompt
            let nextStage = getNextStage currentState.CurrentStage
            {
                Genre = currentState.Genre
                CurrentStage = nextStage
                History = currentState.History @ [nextText] 
                CurrentText = nextText
                Choices = if nextStage = End then [] else nextChoices 
            }

    let initializeStory (genre: Genre) : StoryState =
        let initialState = {
            Genre = genre
            CurrentStage = Introduction
            History = []
            CurrentText = "" // will be filled by AI
            Choices = []
        }
        let initialPrompt = generatePrompt initialState None
        let introText, introChoices = callAiApi initialPrompt
        { initialState with
            CurrentText = introText
            Choices = introChoices
            History = [introText] // start history with the intro
        }

module ConsoleUI =

    let displayState (state: StoryState) : unit =
        printfn "\n========================================"
        printfn $"%s{state.CurrentText}"
        printfn "=========================================="

        if not (List.isEmpty state.Choices) then
            printfn "What do you do next?"
            state.Choices
            |> List.iteri (fun i choice -> printfn $"%d{i + 1}. %s{choice}")
        else
            printfn "\n--- THE END ---"

    let rec getUserChoice (choices: string list) : string =
        if List.isEmpty choices then
             failwith "Cannot get user choice when no choices are available." 

        printf "> "
        match Console.ReadLine() |> Int32.TryParse with
        | true, index when index >= 1 && index <= List.length choices ->
            choices.[index - 1] 
        | _ ->
            printfn $"Invalid input. Please enter a number between 1 and %d{List.length choices}."
            getUserChoice choices // Retry

    let rec selectGenre () : Genre =
        printfn "Choose your adventure's genre:"
        printfn "1. Fantasy"
        printfn "2. Mystery"
        printfn "3. SciFi"
        printfn "4. Romance"
        printfn "5. Comedy"
        printf "> "
        match Console.ReadLine() with
        | "1" -> Fantasy
        | "2" -> Mystery
        | "3" -> SciFi
        | "4" -> Romance
        | "5" -> Comedy
        | _ ->
            printfn "Invalid selection. Please enter 1, 2, or 3."
            selectGenre() // Retry

[<EntryPoint>]
let main argv =
    printfn "Welcome to the F# Interactive Story Generator!"
    let selectedGenre = ConsoleUI.selectGenre()

    let mutable currentState = StoryEngine.initializeStory selectedGenre

    while currentState.CurrentStage <> Stage.End do
        ConsoleUI.displayState currentState
        let userChoice = ConsoleUI.getUserChoice currentState.Choices
        currentState <- StoryEngine.transition currentState userChoice

    ConsoleUI.displayState currentState

    0 // exit code