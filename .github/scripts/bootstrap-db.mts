import yargs from "yargs";
import { hideBin } from "yargs/helpers";

const argv = yargs(hideBin(process.argv))
  .option("deploy-api", {
    description: "Deploy API base path",
  })
  .option("subscription-id", {
    type: "string",
    required: true,
  })
  .option("resource-group", {
    type: "string",
    required: true,
  })
  .option("server-name", {
    type: "string",
    required: true,
  })
  .option("key-vault-name", {
    type: "string",
    required: true,
  })
  .option("user", {
    type: "string",
    required: true,
  })
  .option("database", {
    type: "string",
    required: true,
  })
  .option("role-prefix", {
    type: "string",
    required: true,
  })
  .option("schema", {
    type: "array",
    required: true,
  })
  .parse();

const request = {
  resources: {
    subscriptionId: argv.subscriptionId,
    resourceGroup: argv.resourceGroup,
    serverName: argv.serverName,
    keyVaultName: argv.keyVaultName,
    user: argv.user,
  },
  databaseName: argv.database,
  userPrefix: argv.rolePrefix,
  schemas: Object.fromEntries(argv.schema.map((n) => [n, {}])),
};

// console.log(request);
// process.exit(1);

const baseUrl = new URL(argv.deployApi);
if (baseUrl.protocol === "http:") {
  baseUrl.protocol = "ws:";
} else if (baseUrl.protocol === "https:") {
  baseUrl.protocol = "wss:";
} else {
  throw new Error(`Unsupported protocol: ${baseUrl.protocol}`);
}

const ws = new WebSocket(
  new URL("api/v1/database/bootstrap", baseUrl),
  "altinn.task-pipeline"
);

let pending = Promise.resolve();

const write = (data: Blob) => {
  const buffer = data.arrayBuffer();
  pending = pending
    .then(() => buffer)
    .then((buffer) => {
      var arr = new Uint8Array(buffer);
      process.stderr.write(arr);
    })
    .catch((err) => {
      console.error(err);
      process.exit(1);
    });
};

ws.addEventListener("open", (e) => {
  ws.send(JSON.stringify(request));
});

ws.addEventListener("message", (ev) => {
  write(ev.data);
});

ws.addEventListener("close", (ev) => {
  console.log("closed", ev.code, ev.reason);
  ws.close();

  if (ev.code !== 4000) {
    process.exit(1);
  }
});

ws.addEventListener("error", (ev) => {
  if (ev && "message" in ev) {
    console.error("error", ev.message);
  }
});
