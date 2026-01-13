#!/bin/bash
set -e

echo "Waiting for Kafka to be ready..."
sleep 20

echo "Creating Kafka topics..."

# Create topics
kafka-topics --bootstrap-server kafka:29092 --create --topic saas-subscription-created --partitions 1 --replication-factor 1 --if-not-exists
kafka-topics --bootstrap-server kafka:29092 --create --topic saas-tenant-provisioned --partitions 1 --replication-factor 1 --if-not-exists
kafka-topics --bootstrap-server kafka:29092 --create --topic saas-email-sent --partitions 1 --replication-factor 1 --if-not-exists
kafka-topics --bootstrap-server kafka:29092 --create --topic saas-provision-failed --partitions 1 --replication-factor 1 --if-not-exists
kafka-topics --bootstrap-server kafka:29092 --create --topic saas-email-failed --partitions 1 --replication-factor 1 --if-not-exists

echo "Kafka topics created successfully"
