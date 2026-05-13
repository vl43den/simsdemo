import { EC2Client, StopInstancesCommand } from "@aws-sdk/client-ec2";
import { SNSClient, PublishCommand } from "@aws-sdk/client-sns";

const ec2Client = new EC2Client({ region: "eu-central-1" });
const snsClient = new SNSClient({ region: "eu-central-1" });

export const handler = async (event) => {
    try {
        let body;
        if (event.body) {
            const rawBody = event.isBase64Encoded ? Buffer.from(event.body, 'base64').toString() : event.body;
            body = typeof rawBody === 'string' ? JSON.parse(rawBody) : rawBody;
        } else {
            return { statusCode: 400, body: JSON.stringify({ message: 'Missing request body' }) };
        }

        const resourceId = body.resourceId || body.resource_id;
        if (!resourceId) {
            return { statusCode: 400, body: JSON.stringify({ message: 'Missing resourceId or resource_id' }) };
        }

        console.log(`Attempting to stop instance: ${resourceId}`);

        // Stop the EC2 instance
        const stopParams = {
            InstanceIds: [resourceId],
        };
        const stopCommand = new StopInstancesCommand(stopParams);
        await ec2Client.send(stopCommand);

        // Send an SMS/Email via SNS Topic
        const snsTopicArn = process.env.SNS_TOPIC_ARN;
        if (snsTopicArn) {
            const snsParams = {
                Message: `Incident Escalate: Instance ${resourceId} has been stopped.`,
                TopicArn: snsTopicArn,
            };
            const publishCommand = new PublishCommand(snsParams);
            await snsClient.send(publishCommand);
        }

        return {
            statusCode: 200,
            headers: { "Content-Type": "application/json" },
            body: JSON.stringify({ message: `Successfully stopped instance ${resourceId}` }),
        };
    } catch (error) {
        console.error("Error escalating incident:", error);
        return {
            statusCode: 500,
            headers: { "Content-Type": "application/json" },
            body: JSON.stringify({ message: 'Internal Server Error', error: error.message, name: error.name }),
        };
    }
};
