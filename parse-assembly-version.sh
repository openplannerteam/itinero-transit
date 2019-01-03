
regex="\[assembly: AssemblyInformationalVersion\(\"([0-9]+).([0-9]+).([0-9a-z\-]+)"
while read -r
do [[ $REPLY =~ $regex ]] && echo "##teamcity[buildNumber '${BASH_REMATCH[1]}.${BASH_REMATCH[2]}.${BASH_REMATCH[3]}']"
done < SharedAssemblyVersion.cs
