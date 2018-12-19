$firstLine="// Copyright (c) Microsoft Corporation. All rights reserved."
$secondLine="// Licensed under the MIT License."

[string[]]$Excludes = @('*ProceduralToolkit*', '*Standard Assets*')

Get-ChildItem $PSScriptRoot"\.." -recurse -Include "*.cs" | 
          ? { $_.FullName -inotmatch 'ProceduralToolkit\\' } |
          ? { $_.FullName -inotmatch 'Standard Assets\\' } |
    Foreach-Object {
    $content= Get-Content $_.FullName

    if(($content.length -le 1) -or ($content[0] -ne $firstLine) -or ($content[1] -ne $secondLine))
    {
        @($firstLine,$secondLine,$content) | Set-Content $_.FullName 
    }
}
