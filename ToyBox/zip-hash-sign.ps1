param($game)

#New-Item -ItemType Directory -Force -Path Final

$appPath = (get-item $pwd).parent.parent.FullName
$fileName = "0ToyBox0-" + $game + ".zip"
$fullName = $pwd.Path + "\..\" + $fileName
Compress-Archive -Path '*' -DestinationPath $fullName -Force
#Compress-Archive -Path '*.dll', '*.json', '*.pdb' -DestinationPath $fullName -Force