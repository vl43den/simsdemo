import { EC2Client, StopInstancesCommand } from "@aws-sdk/client-ec2";
import { SNSClient, PublishCommand } from "@aws-sdk/client-sns";

const ec2Client = new EC2Client();
const snsClient = new SNSClient();

export const handler = async (event) => {
    try {
        let body;
        if (event.body) {
            body = typeof event.body === 'string' ? JSON.parse(event.body) : event.body;
        } else {
            return { statusCode: 400, body: 'Missing request body' };
        }

        const resourceId = body.resourceId;
        if (!resourceId) {
            return { statusCode: 400, body: 'Missing resourceId' };
        }

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
            body: JSON.stringify({ message: `Successfully stopped instance ${resourceId}` }),
        };
    } catch (error) {
        console.error("Error escalating incident:", error);
        return {
            statusCode: 500,
            body: JSON.stringify({ message: 'Internal Server Error', error: error.message }),
        };
    }
};
