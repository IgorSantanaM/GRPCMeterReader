const { ReadingPacket, ReadingStatus, ReadingMessage } = require("../proto/reading_pb.js"); 
const { MeterReadingServiceClient } = require("../proto/reading_grpc_web_pb.js");
const { TimeStamp } = require('google-protobuf/google/protobuf/timestamp_pb.js'); 

const theLog = document.getElementById("theLog");
const theButton = document.getElementById("theButton");

function addToLog(msg) {
    const div = document.createElement("div");
    div.innerText = msg;
    theLog.appendChild(div);
}

theButton.addEventListener("click", function () {
    try {
        addToLog("Sending Service Call");
        const packet = new ReadingPacket();
        packet.setSuccessful(ReadingStatus.SUCCESS);

        const reading = new ReadingMessage();
        reading.setCustomerid(1);
        reading.setReadingtime(1000);


        const time = new TimeStamp();
        const now = Date.now();
        time.setSeconds(Math.round(now / 1000));

        reading.setReadingTime(time);

        packet.addReadings(reading);

        addToLog("Packet prepared, creating client");
        const client = new MeterReadingServiceClient(window.location.origin);

        client.addReading(packet, {}, (err, response) => {
            if (err) {
                addToLog("Error code " + err.code + " : " + err.message);
            } else {
                addToLog("Response received from server: ", response.getNotes());
            }
        });
    } catch (err) {
        addToLog("Error: " + err.message);
    }
});