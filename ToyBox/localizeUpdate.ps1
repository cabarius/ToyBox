Import-Module "$env:WrathPath\Wrath_Data\Managed\Newtonsoft.Json.dll"

$root = (get-item $pwd).parent.FullName
$results = New-Object System.Collections.Generic.HashSet[String]
Get-ChildItem -Path $root -Recurse | Foreach-Object {
    if ([IO.Path]::GetExtension($_) -eq ".cs") {
        $string = $_.OpenText().ReadToEnd()
        Select-String '"([^"]*)"\.localize\(\)' -input $string -AllMatches | Foreach-Object {
            $_.Matches | ForEach-Object {
                $results.Add($_.Groups[1].Value) | Out-Null
            }
        }
    }
}
(get-item $pwd).GetDirectories() | ForEach-Object {
    if ($_.Name -eq "Localization") {
        $_.GetFiles() | ForEach-Object {
            if ($_.Name -eq "en.json") {
                $openFile = $_.OpenText()
                $jsonobj = [Newtonsoft.Json.JsonConvert]::DeserializeObject($openFile.ReadToEnd())
                $openFile.Close()
                $results | ForEach-Object {
                    if (!$jsonobj["Strings"].ContainsKey($_)) {
                        $jsonobj["Strings"][$_] = $_
                    }
                }
                $results = $jsonobj["Strings"].Keys
                $out = [Newtonsoft.Json.JsonConvert]::SerializeObject($jsonobj, [Newtonsoft.Json.Formatting]::Indented)
                [System.IO.File]::WriteAllText($_.FullName, $out)
            }
        }
    }
}


(get-item $pwd).GetDirectories() | ForEach-Object {
    if ($_.Name -eq "Localization") {
        $_.GetFiles() | ForEach-Object {
            if (([IO.Path]::GetExtension($_) -eq ".json") -and ($_.Name -ne "en.json")) {
                $openFile = $_.OpenText()
                $jsonobj = [Language][Newtonsoft.Json.JsonConvert]::DeserializeObject($openFile.ReadToEnd())
                $openFile.Close()
                Write-Output($jsonobj)
                $results | ForEach-Object {
                    if (!$jsonobj["Strings"].ContainsKey($_)) {
                       $strings[$_] = ""
                    }
                }
                $out = [Newtonsoft.Json.JsonConvert]::SerializeObject($jsonobj, [Newtonsoft.Json.Formatting]::Indented)
                [System.IO.File]::WriteAllText($_.FullName, $out)
            }
         }
     }
}