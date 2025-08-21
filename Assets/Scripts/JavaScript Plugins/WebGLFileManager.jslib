mergeInto(LibraryManager.library, {
    WebGl_Open: function(gameObjectNamePtr, methodNamePtr) {
        var gameObjectName = UTF8ToString(gameObjectNamePtr);
        var methodName = UTF8ToString(methodNamePtr);
        var input = document.createElement('input');
        input.type = 'file';
        input.accept = '.pbb';
        console.log("Awaiting File")
        input.onchange = e => { 
            console.log("Got File")
            var file = e.target.files[0]; 

            var reader = new FileReader();
            reader.readAsArrayBuffer(file);

            // Catch after file has been selected
            reader.onload = readerEvent => {
                var content = readerEvent.target.result;
                if (content.length == 0) return;
                var bytes = new Uint8Array(content);
                var binary = '';
                for (var i = 0; i < bytes.byteLength; i++) {
                    binary += String.fromCharCode(bytes[i]);
                }
                var base64String = btoa(binary);
                console.log( content );
                SendMessage(gameObjectName, methodName, base64String)
            }
        }
        input.click();
    }
});

