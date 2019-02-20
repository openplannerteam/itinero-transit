webfsd -p 8080
for i in {1..10}
do 
    mono ~/docfx-seed/docfx/docfx.exe build
    echo $i
    sleep 1
done
mono ~/docfx-seed/docfx/docfx.exe metadata
    
./genDocs.sh
