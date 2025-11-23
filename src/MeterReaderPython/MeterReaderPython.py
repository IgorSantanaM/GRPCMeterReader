import grpc;
from meterservice_pb2 import ReadingPacket, ReadingStatus, ReadingMessage;
from google.protobuf.timestamp_pb2 import Timestamp

from meterservice_pb2_grpc import MeterReaderServiceStub;
def main():
    print("Starting Client Call");

    packet = ReadingPacket(Successful = ReadingStatus.Success);

    now = Timestamp();
    nom.GetCurrentTime();

    reading = ReadingMessage(CustomerId = 1, ReadingTime = now, ReadingValue = 10000);

    channel = grpc.insecure_channel("localhost:8888");
    stub = MeterReaderServiceStub(channel)

    response = stub.AddReading(packet);
    if(response.Status == ReadingStatus.Success):
        print("Reading Added Successfully");
    else:
        print("Failed")
main()